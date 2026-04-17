#!/bin/bash

# Variables
## Common
SOURCE="${SOURCE:-https://github.com/logos-storage/logos-storage-nim-cs-dist-tests.git}"
BRANCH="${BRANCH:-master}"
FOLDER="${FOLDER:-/opt/logos-storage-dist-tests}"

## Tests specific
CONTINUOUS_TESTS_FOLDER="${CONTINUOUS_TESTS_FOLDER:-Tests/LogosStorageContinuousTests}"
CONTINUOUS_TESTS_RUNNER="${CONTINUOUS_TESTS_RUNNER:-run.sh}"

# Get code
echo -e "Cloning ${SOURCE} to ${FOLDER}\n"
git clone -b "${BRANCH}" "${SOURCE}" "${FOLDER}"
echo -e "\nChanging folder to ${FOLDER}\n"
cd "${FOLDER}"

# Run tests
echo -e "Running tests from branch '$(git branch --show-current) ($(git rev-parse --short HEAD))'\n"

if [[ "${TESTS_TYPE}" == "continuous-tests" ]]; then
  echo -e "Running continuous-tests\n"
  bash "${CONTINUOUS_TESTS_FOLDER}"/"${CONTINUOUS_TESTS_RUNNER}"
else
  echo -e "Running ${TESTS_TYPE}\n"
  exec "$@"
fi
