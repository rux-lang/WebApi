# Sandbox image for the Rux Playground.
#
# The container receives a single Main.rux mounted read-only at /playground and
# either runs it or dumps its assembly. It carries the Rux compiler plus a
# pre-scaffolded package whose dependency cache is warmed at build time, so the
# container itself never needs network access at run time.

# Ubuntu 24.04 LTS matches the deploy host and ships a glibc/libstdc++ new
# enough for the Rux binary (needs GLIBC_2.38 / GLIBCXX_3.4.31).
FROM ubuntu:24.04

# git is used by `rux install` to fetch packages into the cache.
RUN apt-get update \
 && apt-get install -y --no-install-recommends ca-certificates curl git \
 && rm -rf /var/lib/apt/lists/*

# Install the Rux compiler from the latest Linux release tarball (the archive
# holds the `rux` binary at its root).
ARG RUX_VERSION=latest
RUN if [ "$RUX_VERSION" = "latest" ]; then \
        url="https://github.com/rux-lang/Rux/releases/latest/download/rux-linux.tar.gz"; \
    else \
        url="https://github.com/rux-lang/Rux/releases/download/${RUX_VERSION}/rux-linux.tar.gz"; \
    fi \
 && curl -fsSL -o /tmp/rux.tar.gz "$url" \
 && tar -xzf /tmp/rux.tar.gz -C /usr/local/bin rux \
 && rm /tmp/rux.tar.gz \
 && chmod +x /usr/local/bin/rux

# Run as an unprivileged user; the rootfs is mounted read-only at run time.
RUN useradd --uid 10001 --create-home runner
USER runner
WORKDIR /home/runner

# Pre-scaffold a binary package that depends on Std, install it, and build once.
# This warms ~/.rux/packages so the sandbox resolves Std offline at run time.
# The user's source overwrites Src/Main.rux at run time; Rux.toml is kept.
RUN rux new template --bin \
 && cd template \
 && printf '[Package]\nName = "playground"\nVersion = "0.1.0"\nType = "bin"\n\n[Dependencies]\nStd = "*"\n' > Rux.toml \
 && printf 'import Std::Io::Print;\n\nfunc Main() -> int {\n\tPrint("Ready");\n\treturn 0;\n}\n' > Src/Main.rux \
 && rux install \
 && rux build \
 && rux clean

COPY --chown=runner:runner run-playground.sh /usr/local/bin/run-playground.sh
ENTRYPOINT ["/usr/local/bin/run-playground.sh"]
CMD ["run"]
