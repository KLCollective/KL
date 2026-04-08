-- name: ListUIDsForSecret :many
-- For a secret_key join on the profiles and return all the UIDs that are associated with it.
-- Uses hashed secret key for security
SELECT p.UID, p.alias FROM Profiles p
JOIN Users u ON p.user_id = u.id
WHERE u.secret_key_hash = encode(digest(@secret_key, 'sha256'), 'hex')::text;

-- name: Login :one
-- Confirm whether or not a UID is valid with a secret.
-- If it exists it can be authorized
-- Uses hashed secret key for security
SELECT EXISTS(
    SELECT 1 FROM Profiles p
    JOIN Users u ON p.user_id = u.id
    WHERE p.UID = $1
)::boolean as is_valid;
