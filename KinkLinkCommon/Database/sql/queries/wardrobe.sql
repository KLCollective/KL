-- name: ListWardrobeByProfileId :many
SELECT id, profile_id, name, type, description, slot, relationship_priority, data, created_at, updated_at
FROM wardrobe
WHERE profile_id = $1
ORDER BY relationship_priority DESC, name;

-- name: GetAllWardrobeByType :many
SELECT id, profile_id, name, type, description, slot, relationship_priority, data, created_at, updated_at
FROM wardrobe
WHERE profile_id = $1 AND type = $2
ORDER BY relationship_priority DESC, name;

-- name: GetWardrobeItemByGuid :one
SELECT id, profile_id, name, type, description, slot, relationship_priority, data, created_at, updated_at
FROM wardrobe
WHERE profile_id = $1 AND id = $2;

-- name: CreateOrUpdateWardrobe :one
INSERT INTO wardrobe (id, profile_id, name, type, description, slot, relationship_priority, data, updated_at)
VALUES ($1, $2, $3, $4, $5, $6, $7, $8, NOW())
ON CONFLICT (id) DO UPDATE SET
    name = EXCLUDED.name,
    description = EXCLUDED.description,
    slot = EXCLUDED.slot,
    relationship_priority = EXCLUDED.relationship_priority,
    data = EXCLUDED.data,
    updated_at = NOW()
RETURNING id, profile_id, name, type, description, slot, relationship_priority, data, created_at, updated_at;

-- name: DeleteWardrobe :one
DELETE FROM wardrobe
WHERE profile_id = $1 AND id = $2
RETURNING id, profile_id, name, type, description, slot, relationship_priority, data, created_at, updated_at;

-- name: UpdateWardrobeState :one
INSERT INTO activewardrobe (profile_id, glamourerset, head, body, hand, legs, feet, earring, neck, bracelet, lring, rring, moditems)
VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13)
ON CONFLICT (profile_id) DO UPDATE SET
    glamourerset = EXCLUDED.glamourerset,
    head = EXCLUDED.head,
    body = EXCLUDED.body,
    hand = EXCLUDED.hand,
    legs = EXCLUDED.legs,
    feet = EXCLUDED.feet,
    earring = EXCLUDED.earring,
    neck = EXCLUDED.neck,
    bracelet = EXCLUDED.bracelet,
    lring = EXCLUDED.lring,
    rring = EXCLUDED.rring,
    moditems = EXCLUDED.moditems
RETURNING id, profile_id, glamourerset, head, body, hand, legs, feet, earring, neck, bracelet, lring, rring, moditems;

-- name: GetWardrobeState :one
SELECT id, profile_id, glamourerset, head, body, hand, legs, feet, earring, neck, bracelet, lring, rring, moditems
FROM activewardrobe
WHERE profile_id = $1;
