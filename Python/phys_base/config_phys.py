#
# Universal configuration constants (for all phys modules and programs).
# Placed in this file so that:
# - my prog can be split into >1 module, but have identical values for these
# - insure that when I build release, TESTING=0 for all programs
# Most often changed items are at top


PHYS_BASE_VERSION = '1.2' # update only when building release

# these values are 0 for normal releases (not all used by all programs)
# build file will have "TEST_ONLY" in name if all not zero (VERIFY)
TESTING = 0         # myexit, fnbase
DEV_DLL = 0         # 0=normal 1=development
RUN_TESTS_NOW = 0   # run doc (and unit) tests on main

# TO RUN TESTS
# 1. TESTING = 1 # so that tracedumps are seen in IDLE shell (instead of ERROR.txt!)
#    RUN_TESTS_NOW = 1
# 2. with PsychoBaby-Horn.py open in IDLE, hit F5
#    the doc tests will be run
# To debug:
# 3. can call functions from IDLE shell

AUTHOR = 'Brandon Warren'
CONTACT_EMAIL = 'bwarren@uw.edu'
