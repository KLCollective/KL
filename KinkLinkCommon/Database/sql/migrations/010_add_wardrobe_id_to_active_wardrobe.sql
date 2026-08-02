-- Adds wardrobe_id FK column to active_wardrobe so GetPairWardrobeStateAsync
-- can return real wardrobe item IDs to clients, enabling the Interactions view
-- to display the currently-equipped item for each slot.
--
-- After migration, regenerate: dotnet sqlc generate

ALTER TABLE active_wardrobe
    ADD COLUMN IF NOT EXISTS wardrobe_id UUID REFERENCES wardrobe(id) ON DELETE SET NULL;

-- Backfill existing rows where we can match glamourer_data to a wardrobe item.
-- This is best-effort: if duplicate data exists, the first matching row wins.
UPDATE active_wardrobe aw
SET wardrobe_id = w.id
FROM wardrobe w
WHERE w.profile_id = aw.profile_id
  AND w.data = aw.glamourer_data;

-- Update the NOTIFY trigger to include wardrobe_id (for completeness)
CREATE OR REPLACE FUNCTION notify_active_wardrobe_changed()
RETURNS trigger AS $$
BEGIN
  PERFORM pg_notify('active_wardrobe_changed',
    json_build_object(
      'profile_id', COALESCE(NEW.profile_id, OLD.profile_id),
      'wardrobe_id', COALESCE(NEW.wardrobe_id, OLD.wardrobe_id),
      'action', TG_OP
    )::text);
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;
