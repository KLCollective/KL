-- name: GetAllPairsForProfile :many
-- Will retreive all the permissions data for this profile including the pair IDs which can be used to query their permissions separately
SELECT p.id, p.pair_id
FROM Pairs p
JOIN Profiles p_profile ON p.id = p_profile.id
JOIN Profiles pair_profile ON p.pair_id = pair_profile.id
WHERE p_profile.UID = $1
  AND EXISTS (
      SELECT 1 FROM Pairs p2
      WHERE p2.id = p.pair_id AND p2.pair_id = p.id
  );


-- name: GetOutboundPairRequests :many
-- Returns pairs where the profile sent a request but hasn't received a response yet (one-way pairs)
SELECT p.id, p.pair_id, p.expires, p.priority,
       p.controls_perm, p.controls_config, p.disable_safeword,
        p.interactions,
       pair_profile.UID as pair_uid
FROM Pairs p
JOIN Profiles p_profile ON p.id = p_profile.id
JOIN Profiles pair_profile ON p.pair_id = pair_profile.id
WHERE p_profile.UID = $1
  AND NOT EXISTS (
      SELECT 1 FROM Pairs p2
      WHERE p2.id = p.pair_id AND p2.pair_id = p.id
  );

-- name: GetInboundPairRequests :many
-- Returns pairs where someone sent a request to the profile but they haven't responded yet (one-way pairs)
SELECT p.id, p.pair_id, p.expires, p.priority,
       p.controls_perm, p.controls_config, p.disable_safeword,
        p.interactions,
       p_profile.UID as profile_uid,
       pair_profile.UID as pair_uid
FROM Pairs p
JOIN Profiles p_profile ON p.pair_id = p_profile.id
JOIN Profiles pair_profile ON p.id = pair_profile.id
WHERE p_profile.UID = $1
  AND NOT EXISTS (
      SELECT 1 FROM Pairs p2
      WHERE p2.id = p.pair_id AND p2.pair_id = p.id
  );

-- name: GetPairByProfileIds :one
-- Retrieves a specific pair by profile IDs
SELECT id, pair_id, expires, priority,
       controls_perm, controls_config, disable_safeword,
       interactions
FROM Pairs
WHERE id = $1 AND pair_id = $2;

-- name: ConfirmTwoWayPair :one
-- Checks if both directions of a pair exist in the database.
SELECT (EXISTS(
    SELECT 1 FROM Pairs
    WHERE Pairs.id = $1 AND Pairs.pair_id = $2
) AND EXISTS(
    SELECT 1 FROM Pairs
    WHERE Pairs.id = $2 AND Pairs.pair_id = $1
))::boolean as TwoWayPair;

-- name: AddPair :one
-- This will add a pair without an expiry
INSERT INTO Pairs (id, pair_id, priority, interactions)
VALUES ($1, $2, 0, 0)
RETURNING id, pair_id, expires, priority, controls_perm, controls_config, disable_safeword,
interactions;

-- name: AddTemporaryPair :one
-- This will add a pair with an expiry
INSERT INTO Pairs (id, pair_id, expires)
VALUES ($1, $2, $3)
    ON CONFLICT (id, pair_id)
    DO UPDATE SET expires = $3
RETURNING id, pair_id, expires, priority, controls_perm, controls_config, disable_safeword, interactions;

-- name: SetTemporaryPair :one
INSERT INTO Pairs (id, pair_id, expires)
VALUES ($1, $2, $3)
RETURNING id, pair_id, expires, priority, controls_perm, controls_config, disable_safeword,
interactions;

-- name: RemovePair :one
DELETE FROM Pairs
WHERE (id = $1 AND pair_id = $2) OR (id = $2 AND pair_id = $1)
RETURNING id, pair_id;

-- name: UpdatePairPermissions :one
-- Updates the users permissions granted to the pair.
-- Note: It is possible to give access to a user that is not paired back.
-- These pair permissions are considered to be _pending_ until a two way pair can be confirmed
UPDATE Pairs
SET interactions=$1
WHERE (id = $2 AND pair_id = $3)
RETURNING id, pair_id;

-- name: UpdatePairControlPermissions :one
-- Updates the control permissions for a pair
UPDATE Pairs
SET controls_perm=$1,
    controls_config=$2,
    disable_safeword=$3
WHERE (id = $4 AND pair_id = $5)
RETURNING id, pair_id;

-- name: PurgeExpiredPairs :one
DELETE FROM Pairs
WHERE expires IS NOT NULL AND expires < CURRENT_TIMESTAMP
RETURNING id, pair_id;

-- name: HasExpiredPairs :one
SELECT EXISTS(
    SELECT 1 FROM Pairs
    WHERE expires IS NOT NULL AND expires < CURRENT_TIMESTAMP
)::boolean as has_expired;
