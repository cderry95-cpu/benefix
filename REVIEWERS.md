# Reviewer notes — reasoning and choices

Candid notes on the decisions made during the implementation, what we would
have done differently with more time, and the questions we would have taken
to stakeholders. The short version of each feature's design is in
[`README.md`](README.md); agent-facing conventions live in [`AGENTS.md`](AGENTS.md).

## Authentication and permissions (deliberately out of scope)

The brief states that authentication is not important, so the API is open:
there are no permission checks on who can add, view, update or delete an
employee.

With a login in place, this is what we would have added:

- **Bearer-token authentication** storing the caller's identity on each
  request.
- **User groups / permission checks** so that:
  - administrators can add, update and delete any employee;
  - normal users can view **only their own** profile.
- That would have materialised as a new `userType` column with an enum of
  `Admin` / `Normal`, plus per-endpoint authorisation policies rather than
  scattered role checks.
- We would **not** have changed the data structure for this: the employee
  table itself can stay exactly as it is, and the caller identity lives with
  the authn/authz layer.

If we were building the full login setup instead of open access, the natural
choice would be **SSO** — we would ask stakeholders which providers to support
(e.g. Microsoft Entra / Google / Okta). That offloads the risky parts (storing
passwords, credential rotation, breach detection) to systems far more mature
than anything sensible to build here. If self-hosted auth was required, we
would at minimum add **2FA**.

## Gender: why an enum

Gender is a **fixed set of options**, so we modelled it as a domain enum
(`Male`, `Female`, `Other`, `PreferNotToSay`) rather than a free-form string:

- better for the **frontend**: the allowed values are enumerable
  (the 400 response for an invalid value lists them, so clients can
  self-correct);
- better in the **DB**: stored as a typed value, not a string that can drift
  (`"M"`, `"male"`, `"MALE"`, ...);
- self-documenting in Swagger.

## Employee number: readable integer vs GUID

An additional GUID field would have made IDs even harder to deduce, but the
choice was deliberately made that `EmployeeNo` should be an **easily
human-readable id** — an integer of six random digits. It is **not
sequential** on purpose, so it does not leak how many employees exist or how
fast the company is growing, and uniqueness is still guaranteed by the
database primary key plus the generator's collision-retry loop.

## What we would ask stakeholders about

The user story leaves several field-level truths open; with stakeholder
access we would refine:

- **Address**: do we want structured data (street lines, city, **postcode**)?
  Is it a required field? Today it is an optional free-text field.
- **First/last names**: we assumed they are required — logically an employee
  number needs a name to identify the human. Would stakeholders want
  **min/max length constraints** on them, and how do we treat hyphenated or
  non-Latin names?

## Nice-to-haves (deferred deliberately)

- **Redis caching** for quicker reads (`GET` single/list), with write
  operations (add/update/delete) invalidating or updating the affected cache
  entry so reads never go stale.
- Filtering/search on the list endpoint (surname/email) once the read path
  is proven.
- `201 Created` + `Location` header on POST rather than 200.
