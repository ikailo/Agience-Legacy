import base64
import requests
# import jwt
from jose import jwt

from .constants import TOKEN_API

class Identity:
    def __init__(self, auth_uri, client_id, client_secret, member_id):
        self.auth_uri = auth_uri
        self.client_id = client_id
        self.client_secret = client_secret
        self.member_id = member_id
        self.agency_id = None
        self.tokens = {}

    @property
    def subscribe_member_mask(self):
        return f"{self.agency_id}/{self.member_id}"

    @property
    def subscribe_agency_mask(self):
        return f"{self.agency_id}/0"

    async def authenticate(self, audience):
        print("Authenticating...")
        headers = {
            'Authorization': 'Bearer ' + base64.b64encode(f"{self.client_id}:{self.client_secret}".encode()).decode(),
            'Accept': 'application/json'
        }

        parameters = {
            'grant_type': 'client_credentials',
            'scope': f'member:{self.member_id}',
            'audience': audience
        }

        endpoint = self.auth_uri + TOKEN_API
        response = requests.post(endpoint, headers=headers, json=parameters)

        if response.status_code == 200:
            token_response = response.json()
            print(token_response)

            if token_response is not None:
                # decoded_token = jwt.decode(
                #     token_response['access_token'],
                #     key=self.client_secret,
                #     options={"verify_signature": False}
                # )  # TODO verify signature
                unverified_claims = jwt.get_unverified_claims(token_response['access_token'])
                print(unverified_claims)
                self.agency_id = unverified_claims.get('agency_id')
                self.name = unverified_claims.get('name')
                self.tokens[unverified_claims.get('aud')] = token_response['access_token']

                # for claim in unverified_claims:
                #     print(claim)
                #     if claim == 'agency_id':
                #         self.agency_id = unverified_claims[claim]
                #     if claim == 'name':
                #         self.name = unverified_claims[claim]
                #     if claim == 'aud':
                #         self.tokens[unverified_claims[claim]] = token_response['access_token']
                return

        raise Exception('Unauthorized')
