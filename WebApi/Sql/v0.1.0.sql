CREATE TABLE packages
(
    id          uuid PRIMARY KEY,
    name        character varying        NOT NULL UNIQUE,
    description character varying        NOT NULL,
    repository  character varying        NOT NULL,
    folder      character varying        NOT NULL DEFAULT '',
    license     character varying        NOT NULL,
    created     timestamp with time zone NOT NULL,
    UNIQUE (repository, folder)
);

CREATE TABLE workflows
(
    name              character varying PRIMARY KEY,
    build_conclusion  character varying,
    build_completed   timestamp with time zone,
    test_conclusion   character varying,
    test_completed    timestamp with time zone,
    deploy_conclusion character varying,
    deploy_completed  timestamp with time zone
);
