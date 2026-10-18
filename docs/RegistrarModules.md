# Registrar modules and uploaded notice images

The Admin Dashboard includes the Shifting & Crediting Evaluation, School
Notices & Portal Bulletins, Student Records Directory, and Faculty & Instructor
Management modules. New module tables and demo rows are added by EF Core
migrations; the application applies pending migrations at startup.

## Notice image storage

Images must be JPG, PNG, or WEBP, up to 5 MB. The upload handler checks both
the extension and the image signature and saves each file with a random GUID
filename. Notices store a `/notice-images/...` URL; replaced and deleted
notice images are removed from storage after the database update succeeds.

In production, the default upload folder is `/data/uploads/notices`. In
Development, it is `App_Data/uploads/notices`. For Railway:

1. Add a persistent volume to the service.
2. Mount the volume at `/data`.
3. Deploy the application. It creates the `uploads/notices` subfolder as
   needed.

The folder is ignored by Git. It is served from the configured physical folder
under the `/notice-images` request path; private student uploads continue to
use their existing authenticated route.

The path can be overridden with the `NoticeUploads__Path` environment variable.

## Database provider note

The checked-in project currently uses the EF Core SQLite provider and a local
`smartenroll.db` connection string. The generated module migrations therefore
target SQLite. They add `ShiftingCreditingApplications`, `Notices`,
`Instructors`, and `SubjectSections`; they do not alter or remove any existing
tables or records. If the production Railway service is running a different
PostgreSQL branch, apply these model changes and generate provider-specific
migrations from that PostgreSQL project before deploying them there. Do not
apply the SQLite-generated migration to PostgreSQL.
