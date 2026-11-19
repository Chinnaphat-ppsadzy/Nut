-- Add payment tracking to existing Walk-in queue rows.
-- Run once against the CourtBook database before using the Walk-in payment actions.
ALTER TABLE walk_in_queues
    ADD COLUMN court_fee DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    ADD COLUMN payment_status ENUM('UNPAID','PENDING_VERIFY','REJECTED','PAID') NOT NULL DEFAULT 'UNPAID',
    ADD COLUMN payment_method VARCHAR(20) NULL,
    ADD COLUMN paid_at DATETIME NULL,
    ADD COLUMN paid_by INT UNSIGNED NULL,
    ADD COLUMN slip_image_url VARCHAR(255) NULL;
