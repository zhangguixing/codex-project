-- Apply once after migration_v2.sql to enable threaded activity discussion replies.
ALTER TABLE activity_chat
    ADD COLUMN parent_id BIGINT NULL AFTER user_id;
ALTER TABLE activity_chat
    ADD COLUMN reply_to_user_id BIGINT NULL AFTER parent_id;
