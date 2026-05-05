-- name: CreateNewUIDForUser :one
-- If the provided user exists, create a new UID for them.
INSERT INTO Profiles (user_id, UID, chat_role, alias, title, description)
VALUES ($1, $2, $3, $4, $5, $6)
RETURNING *;

-- name: DeleteProfile :one
-- Should cascade on deletion for any foreign keys
DELETE FROM Profiles
WHERE UID = $1 AND user_id = $2
RETURNING *;

-- name: ProfileExists :one
SELECT EXISTS(
    SELECT 1 FROM Profiles 
    WHERE UID = $1
)::boolean AS exists;

-- name: UpdateAliasForProfile :one
UPDATE Profiles 
SET alias = $1, updated_at = CURRENT_TIMESTAMP
WHERE UID = $2
RETURNING *;

-- name: UpdateDetailsForProfile :one
UPDATE Profiles
SET title = $1, description = $2, alias = $3, chat_role = $4, updated_at = CURRENT_TIMESTAMP
WHERE UID = $5
RETURNING *;

-- name: ListUIDsForUser :many
-- Get all UIDs for a specific user ID
SELECT UID FROM Profiles 
WHERE user_id = $1;

-- name: GetProfileByUid :one
-- Get a profile by its UID
SELECT id, user_id, UID, chat_role, alias, title, description
FROM Profiles 
WHERE UID = $1;
