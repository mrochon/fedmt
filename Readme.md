## Scenario

Use B2C to support a multitenant app whose users are either employees of a federated IdP or local users. Federated users should be signed up silently. Local users are created by federated users.

## SignIn

The sign in UI must display just one federated IdP, chosen by the relying party and local account sign in option. It must also include option to reset local user password.

The sign in url must include two additional query parameters:

- **idp** - with value mapping a claim selecting the right federated IdP TechnicalProfile, e.g. *entraid* or *comp1*.
- **domain** - if the IdP is *entraid*, *domain* value identifies teh Entra ID tenant. 
Example:
```
https://mrochonb2cprod.b2clogin.com/mrochonb2cprod.onmicrosoft.com/oauth2/v2.0/authorize?p=B2C_1A_MT_SIGNIN&client_id=88e4056b-ffce-4660-a3fe-2481e2713197&nonce=defaultNonce&redirect_uri=https%3A%2F%2Foidcdebugger.com%2Fdebug&scope=openid&response_type=id_token&idp=entraid&domain=beitmerari.com
```