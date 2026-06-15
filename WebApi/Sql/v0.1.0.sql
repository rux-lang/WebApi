DROP TABLE IF EXISTS builds;
DROP TABLE IF EXISTS packages;

CREATE TABLE packages
(
    id          uuid PRIMARY KEY,
    name        character varying        NOT NULL UNIQUE,
    description character varying        NOT NULL,
    repository  character varying        NOT NULL UNIQUE,
    license     character varying        NOT NULL,
    created     timestamp with time zone NOT NULL
);

CREATE TABLE builds
(
    id          uuid PRIMARY KEY,
    package_id  uuid REFERENCES packages (id) ON DELETE SET NULL,
    repository  character varying        NOT NULL,
    run_id      bigint                   NOT NULL UNIQUE,
    run_number  integer                  NOT NULL,
    workflow    character varying        NOT NULL,
    branch      character varying        NOT NULL,
    commit      character varying        NOT NULL,
    status      character varying        NOT NULL,
    conclusion  character varying,
    url         character varying        NOT NULL,
    created     timestamp with time zone NOT NULL,
    updated     timestamp with time zone NOT NULL
);

CREATE INDEX builds_package_id_idx ON builds (package_id);
