
CREATE OR REPLACE FUNCTION notify_wardrobe_changed()
RETURNS trigger AS $$
BEGIN
  PERFORM pg_notify('wardrobe_changed',
    json_build_object('profile_id', COALESCE(NEW.profile_id, OLD.profile_id),
                      'action', TG_OP)::text);
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS wardrobe_changes_trigger ON wardrobe;
CREATE TRIGGER wardrobe_changes_trigger
  AFTER INSERT OR UPDATE OR DELETE ON wardrobe
  FOR EACH ROW EXECUTE FUNCTION notify_wardrobe_changed();

CREATE OR REPLACE FUNCTION notify_activewardrobe_changed()
RETURNS trigger AS $$
BEGIN
  PERFORM pg_notify('activewardrobe_changed',
    json_build_object('profile_id', COALESCE(NEW.profile_id, OLD.profile_id),
                      'action', TG_OP)::text);
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS activewardrobe_changes_trigger ON activewardrobe;
CREATE TRIGGER activewardrobe_changes_trigger
  AFTER INSERT OR UPDATE OR DELETE ON activewardrobe
  FOR EACH ROW EXECUTE FUNCTION notify_activewardrobe_changed();

CREATE OR REPLACE FUNCTION notify_lock_changed()
RETURNS trigger AS $$
BEGIN
  PERFORM pg_notify('lock_changed',
    json_build_object('lock_id', COALESCE(NEW.lock_id, OLD.lock_id),
                      'lockee_id', COALESCE(NEW.lockee_id, OLD.lockee_id),
                      'locker_id', COALESCE(NEW.locker_id, OLD.locker_id),
                      'action', TG_OP)::text);
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS locks_changes_trigger ON Locks;
CREATE TRIGGER locks_changes_trigger
  AFTER INSERT OR UPDATE OR DELETE ON Locks
  FOR EACH ROW EXECUTE FUNCTION notify_lock_changed();
