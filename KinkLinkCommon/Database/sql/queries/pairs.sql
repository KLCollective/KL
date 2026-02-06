-- name: GetAllPairsForProfile :many
-- Retreives all currently registered pairs for a Profile
-- This will return the data from both pairs 
SELECT p.*, 
       p1.UID as profile_uid,
       p2.UID as pair_uid
FROM Pairs p
JOIN Profiles p1 ON p.id = p1.id
JOIN Profiles p2 ON p.pair_id = p2.id
WHERE p1.UID = $1 OR p2.UID = $1;

-- name: ConfirmTwoWayPair :one
-- Checks if both directions of a pair exist in the database.
SELECT EXISTS(
    SELECT 1 FROM Pairs 
    WHERE Pairs.id = $1 AND Pairs.pair_id = $2
) AND EXISTS(
    SELECT 1 FROM Pairs 
    WHERE Pairs.id = $2 AND Pairs.pair_id = $1
) as TwoWayPair;

-- name: AddPair :one
-- This will add a pair without an expiry
INSERT INTO Pairs (id, pair_id)
VALUES ($1, $2)
RETURNING *;

-- name: AddTemporaryPair :one
-- This will add a pair with an expiry
INSERT INTO Pairs (id, pair_id, expires)
VALUES ($1, $2, $3)
    ON CONFLICT (id, pair) 
    DO UPDATE SET expires = $3
RETURNING *;

-- name: SetTemporaryPair :one
INSERT INTO Pairs (id, pair_id, expires)
VALUES ($1, $2, $3)
RETURNING *;

-- name: RemovePair :one
DELETE FROM Pairs
WHERE (id = $1 AND pair_id = $2) OR (id = $2 AND pair_id = $1)
RETURNING *;

-- name: UpdatePairPermissions :one
-- Updates the users permissions granted to the pair.
-- Note: It is possible to give access to a user that is not paired back.
-- These pair permissions are considered to be _pending_ until a two way pair can be confirmed
UPDATE Pairs 
SET priority=$1,
    gags=$2,
    wardrobe=$3,
    moodles=$4
WHERE (id = $5 AND pair_id = $6)
RETURNING *;

-- name: PurgeExpiredPairs :one
DELETE FROM Pairs
WHERE expires IS NOT NULL AND expires < CURRENT_TIMESTAMP
RETURNING *;

-- name: HasExpiredPairs :one
SELECT EXISTS(
    SELECT 1 FROM Pairs 
    WHERE expires IS NOT NULL AND expires < CURRENT_TIMESTAMP
);
