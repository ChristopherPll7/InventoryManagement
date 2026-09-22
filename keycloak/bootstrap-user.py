"""Provision the explicitly configured development user through Keycloak Admin REST."""
import json
import os
import sys
import time
from urllib.error import HTTPError, URLError
from urllib.parse import urlencode, quote
from urllib.request import Request, urlopen


def request(base, path, method="GET", body=None, token=None, form=False):
    headers = {}
    if token:
        headers["Authorization"] = "Bearer " + token
    data = None
    if body is not None:
        data = (urlencode(body) if form else json.dumps(body)).encode()
        headers["Content-Type"] = "application/x-www-form-urlencoded" if form else "application/json"
    with urlopen(Request(base + path, data=data, headers=headers, method=method), timeout=10) as response:
        content = response.read()
        return json.loads(content) if content else None


def authenticate(base):
    credentials = {"client_id": "admin-cli", "grant_type": "password",
                   "username": os.environ["KEYCLOAK_ADMIN_USERNAME"],
                   "password": os.environ["KEYCLOAK_ADMIN_PASSWORD"]}
    for attempt in range(60):
        try:
            return request(base, "/realms/master/protocol/openid-connect/token", "POST", credentials, form=True)["access_token"]
        except HTTPError as error:
            if error.code not in (404, 502, 503):
                raise
        except (URLError, TimeoutError):
            pass
        time.sleep(2)
    raise RuntimeError("Keycloak did not become ready within the startup window.")


def configure_client(api, definition):
    clients = api("/clients?" + urlencode({"clientId": definition["clientId"]}))
    if not clients:
        api("/clients", "POST", definition)
        clients = api("/clients?" + urlencode({"clientId": definition["clientId"]}))
    if len(clients) != 1:
        raise RuntimeError("Expected exactly one application client.")
    client_id = clients[0]["id"]
    path = "/clients/" + client_id + "/protocol-mappers/models"
    existing = {mapper["name"]: mapper for mapper in api(path)}
    for mapper in definition.get("protocolMappers", []):
        if mapper["name"] in existing:
            mapper = dict(mapper, id=existing[mapper["name"]]["id"])
            api(path + "/" + mapper["id"], "PUT", mapper)
        else:
            api(path, "POST", mapper)
    return client_id


def provision(api, definition):
    clients = {client["clientId"]: configure_client(api, client) for client in definition["clients"]}
    client_id = clients["inventory-api"]
    roles_path = "/clients/" + client_id + "/roles"
    existing = {role["name"] for role in api(roles_path)}
    desired = definition["roles"]["client"]["inventory-api"]
    for role in desired:
        if role["name"] not in existing:
            api(roles_path, "POST", role)
    roles = [api(roles_path + "/" + quote(role["name"], safe="")) for role in desired]
    # Explicit scope mappings also work when the Swagger client has Full Scope Allowed disabled.
    api("/clients/" + clients["inventory-swagger"] + "/scope-mappings/clients/" + client_id, "POST", roles)
    username = os.environ["INVENTORY_DEV_USERNAME"]
    password = os.environ["INVENTORY_DEV_PASSWORD"]
    if not username or len(password) < 16:
        raise RuntimeError("Configure a development username and a password of at least 16 characters.")
    user_query = "/users?" + urlencode({"username": username, "exact": "true"})
    users = api(user_query)
    created = not users
    if created:
        api("/users", "POST", {"username": username, "enabled": True,
            "firstName": "Inventory", "lastName": "Administrator",
            "email": "admininventory@example.test", "emailVerified": True,
            "credentials": [{"type": "password", "value": password, "temporary": False}]})
        users = api(user_query)
    if len(users) != 1 or not users[0]["enabled"]:
        raise RuntimeError("Expected exactly one enabled development user.")
    path = "/users/" + users[0]["id"] + "/role-mappings/clients/" + client_id
    api(path, "POST", roles)
    if not {role["name"] for role in roles}.issubset({role["name"] for role in api(path)}):
        raise RuntimeError("Development user role verification failed.")
    print("Development user " + ("created" if created else "preserved") + "; all six API permissions verified.")


def main():
    with open("/config/realm.json", encoding="utf-8-sig") as source:
        definition = json.load(source)
    base = os.environ["KEYCLOAK_URL"].rstrip("/")
    token = authenticate(base)
    root = "/admin/realms/" + quote(definition["realm"], safe="")
    api = lambda path, method="GET", body=None: request(base, root + path, method, body, token)
    provision(api, definition)


if __name__ == "__main__":
    try:
        main()
    except HTTPError as error:
        sys.exit("Keycloak bootstrap failed with HTTP " + str(error.code) + "; response omitted to protect credentials.")
    except Exception as error:
        sys.exit("Keycloak bootstrap failed (" + type(error).__name__ + "). Check configuration and service availability.")
