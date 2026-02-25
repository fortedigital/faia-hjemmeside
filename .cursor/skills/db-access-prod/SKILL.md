---
name: db-access-prod
description: Access the production PostgreSQL database on Azure. Use when you need to query, inspect, or modify production schema or data. Requires manual user approval before running any command; never run prod commands without explicit user consent. Use db-access for development instead.
disable-model-invocation: false
---

# Production Database Access

## Purpose
Direct access to the **production** PostgreSQL database (Azure) for:
- Querying production data (read-only preferred)
- Inspecting schema
- Running diagnostic queries
- Verifying data integrity

**Requires manual approval**: Any command that touches production must be proposed for the user to accept before execution. Do not run production database commands automatically without explicit user consent.

## Connection Details

### Production Database (Azure PostgreSQL)
- **Host**: From env var `DB_PROD_HOST` (e.g. `fortedle-db.postgres.database.azure.com`)
- **Port**: `5432`
- **Database**: `fortedle` (app data lives in this database; `postgres` is the default DB and has no app tables)
- **Username**: From env var `DB_PROD_USER` (e.g. `fortedleadmin`)
- **Password**: From env var `DB_PROD_PASSWORD`
- **SSL**: Required (Azure enforces SSL)

### Environment Variables
Set in project `.env` and load before running commands (e.g. `source .env` or run commands with `set -a && source .env && set +a` first):
- `DB_PROD_HOST` – production DB host
- `DB_PROD_USER` – production DB username
- `DB_PROD_PASSWORD` – production DB password

### Connection String (build from env)
```
Host=${DB_PROD_HOST};Port=5432;Database=fortedle;Username=${DB_PROD_USER};Password=${DB_PROD_PASSWORD};SSL Mode=Require
```

## Quick Access Commands

**Load env first** (e.g. `set -a && source .env && set +a` from repo root). **Use `$DB_PROD_HOST`, `$DB_PROD_USER`, `$DB_PROD_PASSWORD`; never paste secrets into the skill or commands.**

### Connect to Database (Interactive psql)
```bash
PGPASSWORD="$DB_PROD_PASSWORD" psql "host=$DB_PROD_HOST port=5432 dbname=fortedle user=$DB_PROD_USER sslmode=require"
```

### Run Single SQL Query
```bash
PGPASSWORD="$DB_PROD_PASSWORD" psql "host=$DB_PROD_HOST port=5432 dbname=fortedle user=$DB_PROD_USER sslmode=require" -c "SELECT version();"
```

### Run SQL from File
```bash
PGPASSWORD="$DB_PROD_PASSWORD" psql "host=$DB_PROD_HOST port=5432 dbname=fortedle user=$DB_PROD_USER sslmode=require" -f query.sql
```

## Common Database Operations

### 1. List All Tables
```bash
PGPASSWORD="$DB_PROD_PASSWORD" psql "host=$DB_PROD_HOST port=5432 dbname=fortedle user=$DB_PROD_USER sslmode=require" -c "\dt"
```

### 2. Describe Table Structure
```bash
PGPASSWORD="$DB_PROD_PASSWORD" psql "host=$DB_PROD_HOST port=5432 dbname=fortedle user=$DB_PROD_USER sslmode=require" -c "\d employees"
```

### 3. Check Migration History
```bash
PGPASSWORD="$DB_PROD_PASSWORD" psql "host=$DB_PROD_HOST port=5432 dbname=fortedle user=$DB_PROD_USER sslmode=require" -c 'SELECT "MigrationId", "ProductVersion" FROM "__EFMigrationsHistory" ORDER BY "MigrationId";'
```

### 4. Count Records (example)
```bash
PGPASSWORD="$DB_PROD_PASSWORD" psql "host=$DB_PROD_HOST port=5432 dbname=fortedle user=$DB_PROD_USER sslmode=require" -c "SELECT COUNT(*) FROM employees;"
```

## Database and tables

Production app data is in **database `fortedle`** (not `postgres`). Use `dbname=fortedle` in all commands. Tables live in the default (public) schema: `leaderboard`, `employees`, `rounds`, `lottery_tickets`, `winning_tickets`, `monthly_winning_tickets`, `lottery_configs`, `employee_weeks`, `giftcard_transactions`, `harvest_tokens`, `__EFMigrationsHistory`, etc.

## Security and Approval

- **Manual approval**: Always present production commands for the user to approve before running. Do not execute against production without explicit consent.
- **Password**: Never include the real password in the skill, in commands committed to the repo, or in chat. Always use `$DB_PROD_PASSWORD` (or similar env) and remind the user to set it.
- **Prefer read-only**: Prefer SELECT and inspection; avoid UPDATE/DELETE unless the user explicitly requests and approves.
- **Audit**: User is responsible for ensuring only authorized production access.

## Troubleshooting

### SSL required
- Azure PostgreSQL requires SSL. Use `sslmode=require` in connection string or the `"host=... sslmode=require"` form in psql.

### Connection timeout / refused
- Check firewall: Azure DB may restrict by IP. Ensure your IP (or VPN) is allowed in the server firewall rules.
- Confirm host, port, and that the server is running.

### Authentication failed
- Verify `DB_PROD_PASSWORD`, `DB_PROD_USER`, and `DB_PROD_HOST` are set in `.env` and loaded (e.g. `source .env`).

## Example Workflows

### Inspect production employees (read-only)
```bash
PGPASSWORD="$DB_PROD_PASSWORD" psql "host=$DB_PROD_HOST port=5432 dbname=fortedle user=$DB_PROD_USER sslmode=require" -c "SELECT id, name, email FROM employees LIMIT 10;"
```

### Inspect production leaderboard (read-only)
```bash
PGPASSWORD="$DB_PROD_PASSWORD" psql "host=$DB_PROD_HOST port=5432 dbname=fortedle user=$DB_PROD_USER sslmode=require" -c "SELECT id, player_name, score, date FROM leaderboard ORDER BY date DESC LIMIT 20;"
```

### Check production migration state
```bash
PGPASSWORD="$DB_PROD_PASSWORD" psql "host=$DB_PROD_HOST port=5432 dbname=fortedle user=$DB_PROD_USER sslmode=require" -c 'SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId";'
```
