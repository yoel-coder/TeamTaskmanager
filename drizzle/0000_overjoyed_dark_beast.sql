CREATE TABLE `tasks` (
	`id` integer PRIMARY KEY AUTOINCREMENT NOT NULL,
	`title` text NOT NULL,
	`description` text,
	`status` text DEFAULT 'Open' NOT NULL,
	`created_by` text NOT NULL,
	`created_at` text NOT NULL,
	`assigned_to` text,
	`started_at` text,
	`completed_by` text,
	`completed_at` text
);
