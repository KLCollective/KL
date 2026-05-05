ALTER TABLE Locks DROP CONSTRAINT IF EXISTS locks_lockee_id_fkey;
ALTER TABLE Locks DROP CONSTRAINT IF EXISTS locks_locker_id_fkey;

ALTER TABLE Locks ADD CONSTRAINT locks_lockee_id_fkey
  FOREIGN KEY (lockee_id) REFERENCES Profiles(id) ON DELETE CASCADE;
ALTER TABLE Locks ADD CONSTRAINT locks_locker_id_fkey
  FOREIGN KEY (locker_id) REFERENCES Profiles(id) ON DELETE CASCADE;
