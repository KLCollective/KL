-- name: InsertTestUser :one
-- Inserts a test user into the Users table
INSERT INTO Users (discord_id, secret_key_hash, verified, banned)
VALUES ($1, $2, COALESCE($3, false)::boolean, COALESCE($4, false)::boolean)
RETURNING id;

-- name: InsertTestProfile :one
-- Inserts a test profile into the Profiles table
INSERT INTO Profiles (user_id, uid, chat_role, alias, title, description)
VALUES ($1, $2, COALESCE($3, '')::varchar, COALESCE($4, '')::varchar, COALESCE($5, 'Kinkster')::varchar, COALESCE($6, '')::varchar)
RETURNING id;

-- name: InsertTestPair :one
-- Inserts a test pair relationship between two profiles
INSERT INTO Pairs (id, pair_id, priority, interactions)
VALUES ($1, $2, COALESCE($3, 0)::integer, COALESCE($4, 0)::integer)
RETURNING *;

-- name: InsertTestPairWithPermissions :one
-- Inserts a test pair with specific permission values
INSERT INTO Pairs (id, pair_id, expires, priority, interactions)
VALUES ($1, $2, $3, $4, $5)
RETURNING *;

-- name: DeleteTestUserByDiscordId :one
-- Deletes a user and all related profiles and pairs by discord_id
DELETE FROM Users WHERE Users.discord_id = $1
RETURNING *;

-- name: DeleteTestProfileByUid :one
-- Deletes a profile by UID
DELETE FROM Profiles WHERE uid = $1
RETURNING *;

-- name: DeleteTestPair :one
-- Deletes a pair relationship between two profile IDs
DELETE FROM Pairs WHERE (id = $1 AND pair_id = $2) OR (id = $2 AND pair_id = $1)
RETURNING *;

-- name: GetProfileIdByUid :one
-- Gets the profile ID from a UID
SELECT id FROM Profiles WHERE uid = $1;

-- name: GetUserIdByDiscordId :one
-- Gets the user ID from a discord_id
SELECT id FROM Users WHERE discord_id = $1;

-- name: TableExists :one
-- Checks if a table exists in the database
SELECT EXISTS (
    SELECT 1 FROM information_schema.tables 
    WHERE table_schema = 'public' 
    AND table_name = $1::varchar
)::boolean as exists;

-- name: ColumnExists :one
-- Checks if a column exists in a table
SELECT EXISTS (
    SELECT 1 FROM information_schema.columns 
    WHERE table_schema = 'public' 
    AND table_name = $1::varchar
    AND column_name = $2::varchar
)::boolean as exists;

-- name: TruncateTables :exec
-- Truncates all tables in correct order for test isolation
TRUNCATE TABLE Pairs, Profiles, Users RESTART IDENTITY CASCADE;

-- name: SeedUser :one
-- Seeds a test user (idempotent - updates on conflict)
INSERT INTO Users (discord_id, secret_key_hash, verified, banned)
VALUES ($1, $2, $3, $4)
ON CONFLICT (discord_id) DO UPDATE SET
    secret_key_hash = EXCLUDED.secret_key_hash,
    verified = EXCLUDED.verified,
    banned = EXCLUDED.banned
RETURNING id;

-- name: SeedProfile :one
-- Seeds a test profile (idempotent)
INSERT INTO Profiles (user_id, uid, chat_role, alias, title, description)
VALUES ($1, $2, $3, $4, $5, $6)
ON CONFLICT (uid) DO UPDATE SET
    chat_role = EXCLUDED.chat_role,
    alias = EXCLUDED.alias,
    title = EXCLUDED.title,
    description = EXCLUDED.description
RETURNING id;

-- name: SeedProfileConfig :one
-- Seeds profile config (idempotent)
INSERT INTO ProfileConfig (id, enable_glamours, enable_garbler, enable_garbler_channels, enable_moodles)
VALUES ($1, $2, $3, $4, $5)
ON CONFLICT (id) DO UPDATE SET
    enable_glamours = EXCLUDED.enable_glamours,
    enable_garbler = EXCLUDED.enable_garbler,
    enable_garbler_channels = EXCLUDED.enable_garbler_channels,
    enable_moodles = EXCLUDED.enable_moodles
RETURNING id;

-- name: SeedPair :one
-- Seeds a pair relationship (idempotent)
INSERT INTO Pairs (id, pair_id, priority, controls_perm, controls_config, disable_safeword, interactions)
VALUES ($1, $2, $3, $4, $5, $6, $7)
ON CONFLICT (id, pair_id) DO UPDATE SET
    priority = EXCLUDED.priority,
    controls_perm = EXCLUDED.controls_perm,
    controls_config = EXCLUDED.controls_config,
    disable_safeword = EXCLUDED.disable_safeword,
    interactions = EXCLUDED.interactions
RETURNING id, pair_id;
