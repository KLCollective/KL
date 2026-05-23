-- name: ListWardrobeByProfileId :many
SELECT id, profile_id, name, layer, description, relationship_priority, data, created_at, updated_at
FROM wardrobe
WHERE profile_id = $1
ORDER BY relationship_priority DESC, name;

-- name: GetAllWardrobeByType :many
SELECT id, profile_id, name, layer, description, relationship_priority, data, created_at, updated_at
FROM wardrobe
WHERE profile_id = $1 AND layer = $2
ORDER BY relationship_priority DESC, name;

-- name: GetWardrobeItemByGuid :one
SELECT id, profile_id, name, layer, description, relationship_priority, data, created_at, updated_at
FROM wardrobe
WHERE profile_id = $1 AND id = $2;

-- name: CreateOrUpdateWardrobe :one
INSERT INTO wardrobe (id, profile_id, name, layer, description, relationship_priority, data, updated_at)
VALUES ($1, $2, $3, $4, $5, $6, $7, NOW())
ON CONFLICT (id) DO UPDATE SET
    name = EXCLUDED.name,
    description = EXCLUDED.description,
    layer = EXCLUDED.layer,
    relationship_priority = EXCLUDED.relationship_priority,
    data = EXCLUDED.data,
    updated_at = NOW()
RETURNING id, profile_id, name, layer, description, relationship_priority, data, created_at, updated_at;

-- name: DeleteWardrobe :exec
DELETE FROM wardrobe
WHERE profile_id = $1 AND id = $2;

-- name: UpdateWardrobeState :one
INSERT INTO active_wardrobe (
    profile_id,
    layer,
    glamourer_data
)
VALUES ($1, $2, $3)
ON CONFLICT (profile_id, layer) DO UPDATE SET
    glamourer_data = EXCLUDED.glamourer_data
RETURNING profile_id, layer;

-- name: ClearWardrobeLayer :exec
DELETE FROM active_wardrobe
WHERE profile_id = $1 AND layer = $2;

-- name: GetWardrobeState :many
SELECT profile_id, layer, glamourer_data
FROM active_wardrobe
WHERE profile_id = $1;

-- name: ClearWardrobeState :exec
DELETE FROM active_wardrobe WHERE profile_id = $1;
