# mydyndns
A custom dynamic DNS based on Docker, Azure Functions and CloudFlare

## Usage

### Deploy client

```
docker run -d --name mydyndns \
--restart=always \
-e "AUTOPUDATE_REFRESH=60"  \
-e "AUTOUPDATE_ENDPOINT=https://myendpoint.azurewebsites.net/api/update/maneu.net"  \
docker.pkg.github.com/cmaneu/mydyndns/mydyndns-client:latest
```
