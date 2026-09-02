# RaceDay System - API Endpoint Plan

This document lists every API endpoint the RaceDay system will expose,
covering Authentication, User Profile, Events, Categories, Event
Enrolments, and Results, as required by the Functional Requirements
in Part 2. This plan must be implemented as-is in Part 2; any
deviation will be explained in the README.

**Roles:** `Organiser`, `Participant`

---

## Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Registers a new user account and assigns their chosen role (Organiser or Participant). | None (public) | `{ fullName, email, password, role }` | 201 Created - new user id and role<br>400 Bad Request - validation failed<br>409 Conflict - email already registered |
| POST | /api/auth/login | Authenticates a user and starts a session, storing the user's id and role for subsequent requests. | None (public) | `{ email, password }` | 200 OK - session established, user id and role returned<br>401 Unauthorized - invalid credentials |
| POST | /api/auth/logout | Ends the current user's session. | Any (logged in) | None | 200 OK - session cleared |

## User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/users/me | Returns the profile information of the currently logged-in user. | Any (logged in) | None | 200 OK - user profile object<br>401 Unauthorized - no active session |
| PUT | /api/users/me | Updates the profile information of the currently logged-in user. | Any (logged in) | `{ fullName, email }` | 200 OK - updated profile<br>400 Bad Request - validation failed<br>401 Unauthorized |

## Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events | Lists all events. Visible to both roles. | Any (logged in) | None | 200 OK - array of events |
| GET | /api/events/{id} | Returns the details of a single event. | Any (logged in) | None | 200 OK - event object<br>404 Not Found - event does not exist |
| POST | /api/events | Creates a new event. Only Organisers may create events. | Organiser | `{ name, description, eventDate, location, distance, eventType }` | 201 Created - new event id<br>400 Bad Request - validation failed<br>403 Forbidden - not an Organiser |
| PUT | /api/events/{id} | Updates an existing event owned by the logged-in Organiser. | Organiser | `{ name, description, eventDate, location, distance, eventType }` | 200 OK - updated event<br>403 Forbidden - not the owning Organiser<br>404 Not Found |
| DELETE | /api/events/{id} | Deletes an event owned by the logged-in Organiser. | Organiser | None | 204 No Content<br>403 Forbidden - not the owning Organiser<br>404 Not Found |

## Categories

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events/{eventId}/categories | Lists all categories available for a specific event. | Any (logged in) | None | 200 OK - array of categories<br>404 Not Found - event does not exist |
| POST | /api/events/{eventId}/categories | Defines a new age or distance category for an event owned by the logged-in Organiser. | Organiser | `{ categoryName, minAge, maxAge, categoryDistance }` | 201 Created - new category id<br>400 Bad Request - validation failed<br>403 Forbidden - not the owning Organiser<br>404 Not Found - event does not exist |

## Event Enrolments

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/events/{eventId}/enrolments | Enrols the logged-in Participant into the event under a selected category. | Participant | `{ categoryId }` | 201 Created - enrolment record<br>400 Bad Request - invalid category for this event<br>403 Forbidden - not a Participant<br>404 Not Found - event or category does not exist<br>409 Conflict - already enrolled in this event |
| GET | /api/events/{eventId}/enrolments | Lists all enrolments for an event, visible to the owning Organiser. | Organiser | None | 200 OK - array of enrolments<br>403 Forbidden - not the owning Organiser<br>404 Not Found - event does not exist |
| GET | /api/users/me/enrolments | Lists the logged-in Participant's own enrolments. | Participant | None | 200 OK - array of enrolments |

## Results

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/enrolments/{enrolmentId}/results | Captures the finish time and finishing position for a Participant's enrolment. Restricted to the Organiser who owns the related event. | Organiser | `{ finishTime, finishPosition }` | 201 Created - result record<br>400 Bad Request - validation failed<br>403 Forbidden - not the owning Organiser<br>404 Not Found - enrolment does not exist<br>409 Conflict - result already captured for this enrolment |
| GET | /api/users/me/results | Returns the logged-in Participant's own results across all events. | Participant | None | 200 OK - array of results |
| GET | /api/events/{eventId}/results | Returns all results for an event, visible to the owning Organiser. | Organiser | None | 200 OK - array of results<br>403 Forbidden - not the owning Organiser<br>404 Not Found - event does not exist |
