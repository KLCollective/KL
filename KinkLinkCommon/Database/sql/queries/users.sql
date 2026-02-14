-- name: RegisterNewUser :one
-- If it doesn't exist, register a new user.
-- Uses hashed secret for security
INSERT INTO Users (discord_id, secret_key_hash)
VALUES (@discord_id, encode(digest(@secret_key, 'sha256'), 'hex'))
ON CONFLICT (discord_id) DO NOTHING
RETURNING *;

-- name: RegenerateSecretKey :exec
-- Allows users to get a new secret key from the service.
-- Uses hashed secret for security
UPDATE Users
SET secret_key_hash=encode(digest(@secret_key, 'sha256'), 'hex')
WHERE discord_id=$1;

-- name: DeleteUserAccount :one
-- Deletes a user account and cascades to all data referencing the user.
DELETE FROM Users
WHERE discord_id = $1
RETURNING *;

-- name: SelectUserbyDiscordId :one
SELECT * FROM Users
WHERE discord_id = $1;

-- name: UserExists :one
SELECT EXISTS(
    SELECT 1 FROM Users 
    WHERE discord_id = $1
);
