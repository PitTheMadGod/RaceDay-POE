RaceDay System - Part 1: System Planning and Database
System Description
RaceDay is a system for managing running, walking, and cycling events.
Organisers create and manage events, defining the date, location,
distance, and event type, along with the age or distance categories
that participants can enter (e.g. Under 20, Senior, 10km, 21km).
Participants register for an account, browse available events, and
enrol in an event under a category of their choice. After an event
takes place, Organisers capture each participant's finish time and
finishing position, which participants can then view against their
own profile.
This part of the project covers the planning and database layer that
the RESTful API (Part 2) and MVC front end (Part 3) will be built on:
an Entity Relationship Diagram, an API endpoint plan, and a SQL
script that creates and seeds the database schema.
Roles
Organiser
An Organiser creates, updates, and deletes events, defines the
categories available for each event, views who has enrolled in their
events, and captures results (finish time and position) for
participants after the event has taken place.
Participant
A Participant browses available events and categories, enrols in an
event under a chosen category, and views their own results once an
Organiser has captured them. A Participant cannot manage events or
enter results for other participants.
Repository Structure
/docs
RaceDay_ERD.png - Entity Relationship Diagram
RaceDay_Endpoint_Plan.md - API endpoint plan
RaceDay_Database_Script.sql - Database creation and seed script
ci-success-screenshot.png - CI/CD passing build screenshot
README.md
CI/CD
A GitHub Actions workflow validates the repository structure on every
push, checking that the /docs folder exists and contains the
required files.
Successful build screenshot:
�
Load image
Video Walkthrough
An unlisted YouTube video walking through the planning documents, the
ERD decisions, the endpoint plan choices, and running the SQL script
live in SSMS:
Watch here
