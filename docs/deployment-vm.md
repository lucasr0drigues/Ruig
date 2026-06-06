# Ruig VM Docker Deployment

This deployment runs Ruig with Docker Compose:

- `caddy`: public HTTPS reverse proxy on ports `80` and `443`
- `api`: ASP.NET Core app on the private Compose network
- `postgres`: PostgreSQL on the private Compose network
- `migrate`: one-shot EF migration service

## 1. Prepare the VM

Install Docker Engine and the Docker Compose plugin on an Ubuntu LTS VM.

Open only these inbound ports:

```text
22/tcp
80/tcp
443/tcp
```

Point the production DNS record at the VM before starting Caddy.

## 2. Create the production env file

Create `/etc/ruig/ruig.env` from `deploy/ruig.env.example`:

```bash
sudo mkdir -p /etc/ruig
sudo cp deploy/ruig.env.example /etc/ruig/ruig.env
sudo chmod 600 /etc/ruig/ruig.env
```

Fill every `REPLACE_...` value. Keep these values backed up securely:

- `POSTGRES_PASSWORD`
- `Strava__ClientSecret`
- `Strava__WebhookVerifyToken`
- `GitHub__AccessToken`
- `TokenEncryption__Keys__v1`

Generate a token encryption key with:

```bash
openssl rand -hex 32
```

`ConnectionStrings__Default` must use `Host=postgres`, because the API reaches PostgreSQL through the Compose network.

For the first boot, `Strava__WebhookSubscriptionId` may be set to any positive temporary value, such as `1`. The webhook verification `GET` does not use the subscription id. Replace it with the real Strava subscription id immediately after registering the webhook.

### GitHub token rotation

`GitHub__AccessToken` is a GitHub personal access token used by the badge renderer to query GitHub GraphQL contribution data. Do not commit the token value; store it only in `/etc/ruig/ruig.env` on the VM or in the secure backup for production secrets.

The production GitHub token was rotated on 2026-06-06 and expires on 2027-06-06. Set a reminder to rotate it before that date. If the token expires or is revoked, badge SVG requests will return `500` and the API logs will show `POST https://api.github.com/graphql` followed by `401 Unauthorized`.

To rotate it:

1. Generate a new GitHub personal access token at `https://github.com/settings/personal-access-tokens/new`.
2. Update `GitHub__AccessToken` in `/etc/ruig/ruig.env`.
3. Restart the API:

```bash
docker compose up -d api
```

4. Confirm a badge URL returns `200 OK`:

```bash
curl -sS -D - -o /dev/null "https://<your-domain>/badges/<slug>.svg"
```

## 3. Build and start PostgreSQL

```bash
docker compose up -d postgres
```

Wait until PostgreSQL is healthy:

```bash
docker compose ps
```

## 4. Apply migrations

```bash
docker compose --profile tools run --rm migrate
```

Run this again after future deploys that include EF migrations.

## 5. Start the application

```bash
docker compose up -d --build api caddy
```

Check status:

```bash
docker compose ps
docker compose logs --tail=100 api
docker compose logs --tail=100 caddy
```

## 6. Configure Strava

Set the Strava callback URL to:

```text
https://<your-domain>/auth/strava/callback
```

Register the Strava webhook callback at:

```text
https://<your-domain>/webhooks/strava
```

Store the returned subscription id in:

```text
Strava__WebhookSubscriptionId
```

Then restart the app:

```bash
docker compose up -d api
```

Caddy access logging is intentionally not enabled in `deploy/Caddyfile`, so OAuth callback query strings are not written to Caddy access logs.

## 7. Smoke test

```bash
curl -f https://<your-domain>/healthz
curl -f https://<your-domain>/badges/styles
```

Then run the full browser flow:

1. Open `https://<your-domain>/`.
2. Enter a GitHub username.
3. Authorize Strava.
4. Confirm the generated badge URL renders.

## 8. Backups

Back up PostgreSQL and the token encryption key together. A database backup without `TokenEncryption__Keys__v1` cannot decrypt saved Strava tokens.

Example database backup:

```bash
docker compose exec -T postgres sh -c 'pg_dump -U "$POSTGRES_USER" "$POSTGRES_DB"' > ruig-$(date +%F).sql
```

Caddy certificates live in the `caddy_data` volume. PostgreSQL data lives in the `postgres_data` volume.
