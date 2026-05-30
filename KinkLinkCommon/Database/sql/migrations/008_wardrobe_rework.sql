-- Drop active_wardrobe_state
-- Note: older schema used 'activewardrobe' (no underscore). DROP targets that name intentionally for backward cleanup.
DROP TABLE IF EXISTS activewardrobe;

-- Recreate it with appropriate columns
CREATE TABLE IF NOT EXISTS active_wardrobe (
    profile_id INTEGER NOT NULL REFERENCES Profiles(id) ON DELETE CASCADE,
    layer INTEGER NOT NULL DEFAULT 0,
    glamourer_data TEXT NOT NULL, -- If the layer is empty it should be deleted
    PRIMARY KEY (profile_id, layer) -- composite primary key on layer and profile_id
);

-- Remove wardrobe rows where type is not 'set'
-- Be careful: IS DISTINCT FROM treats NULL as distinct. Only remove rows where type is explicitly non-null and not 'set'.
DELETE FROM wardrobe WHERE type IS NOT NULL AND type <> 'set';

ALTER TABLE wardrobe DROP CONSTRAINT IF EXISTS valid_type;
-- Drop column 'type' from wardrobe (no longer needed)
ALTER TABLE wardrobe DROP COLUMN IF EXISTS type;
ALTER TABLE wardrobe DROP COLUMN IF EXISTS slot;

-- Create column 'layer' on wardrobe and default to 0
ALTER TABLE wardrobe ADD COLUMN IF NOT EXISTS layer INTEGER NOT NULL DEFAULT 0;

-- Ensure existing rows have layer = 0
UPDATE wardrobe SET layer = 0 WHERE layer IS DISTINCT FROM 0;
