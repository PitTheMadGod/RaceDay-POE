/* ============================================================
   RaceDay System - Database Creation Script
   Author: [Your Name]
   Student Number: [Your Student Number]
   Module: PROG6212 - POE Part 1, Section C
   Tool: SQL Server Management Studio (SSMS)

   This script creates the full database schema for the RaceDay
   system and matches the ERD in /docs/RaceDay_ERD.png exactly.

   Run this script on a clean SQL Server instance from top to
   bottom. It is safe to re-run: existing objects are dropped
   first.
   ============================================================ */

IF DB_ID('RaceDayDB') IS NOT NULL
BEGIN
    ALTER DATABASE RaceDayDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE RaceDayDB;
END
GO

CREATE DATABASE RaceDayDB;
GO

USE RaceDayDB;
GO

/* ============================================================
   TABLE: Roles
   Lookup table for the two application roles. Kept as its own
   table (rather than a hard-coded string on Users) so that the
   role-based design is explicit and enforced with a foreign key.
   ============================================================ */
CREATE TABLE Roles (
    RoleId      INT IDENTITY(1,1) PRIMARY KEY,
    RoleName    NVARCHAR(20) NOT NULL UNIQUE
);
GO

/* ============================================================
   TABLE: Users
   Stores both Organisers and Participants. The RoleId foreign
   key determines which role the account has.
   ============================================================ */
CREATE TABLE Users (
    UserId          INT IDENTITY(1,1) PRIMARY KEY,
    FullName        NVARCHAR(100)   NOT NULL,
    Email           NVARCHAR(150)   NOT NULL UNIQUE,
    PasswordHash    NVARCHAR(255)   NOT NULL,
    RoleId          INT             NOT NULL,
    CreatedAt       DATETIME2       NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
);
GO

/* ============================================================
   TABLE: Events
   Created and managed by an Organiser (a User with the
   Organiser role). Each event stores the details required by
   the functional requirements: name, description, date,
   location, distance, and event type.
   ============================================================ */
CREATE TABLE Events (
    EventId         INT IDENTITY(1,1) PRIMARY KEY,
    Name            NVARCHAR(150)   NOT NULL,
    Description     NVARCHAR(1000)  NULL,
    EventDate       DATE            NOT NULL,
    Location        NVARCHAR(150)   NOT NULL,
    Distance        DECIMAL(6,2)    NOT NULL,
    EventType       NVARCHAR(10)    NOT NULL,
    OrganiserId     INT             NOT NULL,
    CONSTRAINT FK_Events_Users FOREIGN KEY (OrganiserId) REFERENCES Users(UserId),
    CONSTRAINT CK_Events_EventType CHECK (EventType IN ('Run', 'Walk', 'Cycle'))
);
GO

/* ============================================================
   TABLE: Categories
   Age or distance categories defined per event by the
   Organiser (e.g. Under 20, Senior, 10km, 21km).
   ============================================================ */
CREATE TABLE Categories (
    CategoryId      INT IDENTITY(1,1) PRIMARY KEY,
    EventId         INT             NOT NULL,
    CategoryName    NVARCHAR(50)    NOT NULL,
    MinAge          INT             NULL,
    MaxAge          INT             NULL,
    CategoryDistance DECIMAL(6,2)   NULL,
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventId) REFERENCES Events(EventId)
);
GO

/* ============================================================
   TABLE: EventEnrolments
   Links a Participant to an Event and the Category they
   selected. This is the resolved many-to-many relationship
   between Users (Participants) and Events, via Categories.
   ============================================================ */
CREATE TABLE EventEnrolments (
    EnrolmentId     INT IDENTITY(1,1) PRIMARY KEY,
    EventId         INT             NOT NULL,
    CategoryId      INT             NOT NULL,
    ParticipantId   INT             NOT NULL,
    EnrolmentDate   DATETIME2       NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Enrolments_Events FOREIGN KEY (EventId) REFERENCES Events(EventId),
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId),
    CONSTRAINT FK_Enrolments_Users FOREIGN KEY (ParticipantId) REFERENCES Users(UserId),
    CONSTRAINT UQ_Enrolment_ParticipantEvent UNIQUE (EventId, ParticipantId)
);
GO

/* ============================================================
   TABLE: Results
   Finish time and finishing position for a Participant's
   enrolment in an event, captured by the Organiser after the
   event. One-to-one with EventEnrolments.
   ============================================================ */
CREATE TABLE Results (
    ResultId        INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentId     INT             NOT NULL UNIQUE,
    FinishTime      TIME(0)         NOT NULL,
    FinishPosition  INT             NOT NULL,
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentId) REFERENCES EventEnrolments(EnrolmentId)
);
GO

/* ============================================================
   SEED DATA
   ============================================================ */

-- Roles
INSERT INTO Roles (RoleName) VALUES ('Organiser'), ('Participant');
GO

-- Users: 1 Organiser + 2 Participants (minimum required)
-- NOTE: PasswordHash values below are placeholders representing
-- a hashed password (e.g. BCrypt output), not plain text.
INSERT INTO Users (FullName, Email, PasswordHash, RoleId)
VALUES
('Sarah Mokoena', 'sarah.mokoena@raceday.co.za', 'AQAAAAIAAYagAAAAEHash1==', 1), -- Organiser
('Thabo Nkosi', 'thabo.nkosi@example.com', 'AQAAAAIAAYagAAAAEHash2==', 2),      -- Participant
('Lindiwe Dube', 'lindiwe.dube@example.com', 'AQAAAAIAAYagAAAAEHash3==', 2);    -- Participant
GO

-- Events: 3 events, all organised by Sarah Mokoena (UserId 1)
INSERT INTO Events (Name, Description, EventDate, Location, Distance, EventType, OrganiserId)
VALUES
('Johannesburg City Run', 'Annual road run through the Johannesburg CBD.', '2026-10-10', 'Johannesburg', 21.10, 'Run', 1),
('Durban Beachfront Cycle', 'Scenic cycling race along the Durban beachfront.', '2026-11-02', 'Durban', 40.00, 'Cycle', 1),
('Cape Town Family Walk', 'Community fun walk for all ages.', '2026-09-20', 'Cape Town', 5.00, 'Walk', 1);
GO

-- Categories: for each event
INSERT INTO Categories (EventId, CategoryName, MinAge, MaxAge, CategoryDistance)
VALUES
(1, '10km', NULL, NULL, 10.00),
(1, '21km', NULL, NULL, 21.10),
(2, '40km Open', 18, NULL, 40.00),
(3, 'Under 20', NULL, 19, 5.00),
(3, 'Senior', 20, NULL, 5.00);
GO

-- Sample enrolments: both participants enter events
INSERT INTO EventEnrolments (EventId, CategoryId, ParticipantId)
VALUES
(1, 1, 2),  -- Thabo enters JHB City Run, 10km
(1, 2, 3),  -- Lindiwe enters JHB City Run, 21km
(3, 5, 2);  -- Thabo enters Cape Town Family Walk, Senior
GO

-- Sample results for a completed enrolment
INSERT INTO Results (EnrolmentId, FinishTime, FinishPosition)
VALUES
(1, '00:52:14', 15);
GO
