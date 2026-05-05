-- name: CreateOrUpdateProfileConfig :one
-- Creates or updates a profile config
INSERT INTO ProfileConfig (id, enable_glamours, enable_garbler, enable_garbler_channels, enable_moodles)
VALUES ($1, $2, $3, $4, $5)
ON CONFLICT (id) DO UPDATE SET
    enable_glamours = EXCLUDED.enable_glamours,
    enable_garbler = EXCLUDED.enable_garbler,
    enable_garbler_channels = EXCLUDED.enable_garbler_channels,
    enable_moodles = EXCLUDED.enable_moodles
RETURNING id, enable_glamours, enable_garbler, enable_garbler_channels, enable_moodles;

-- name: GetProfileConfigById :one
-- Gets a profile config by profile ID
SELECT id, enable_glamours, enable_garbler, enable_garbler_channels, enable_moodles
FROM ProfileConfig
WHERE id = $1;

-- name: GetProfileConfigByUid :one
-- Gets a profile config by profile UID
SELECT pc.id, pc.enable_glamours, pc.enable_garbler, pc.enable_garbler_channels, pc.enable_moodles
FROM ProfileConfig pc
JOIN Profiles p ON pc.id = p.id
WHERE p.UID = $1;

-- name: DeleteProfileConfig :one
-- Deletes a profile config by profile ID
DELETE FROM ProfileConfig
WHERE id = $1
RETURNING id, enable_glamours, enable_garbler, enable_garbler_channels, enable_moodles;

-- name: ListProfileConfigs :many
-- Lists all profile configs
SELECT id, enable_glamours, enable_garbler, enable_garbler_channels, enable_moodles
FROM ProfileConfig;
