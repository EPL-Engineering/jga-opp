#
# 'Brandon' = Win7 'Brandon_TDT' = TDT system

SYSTEM = 'Arenberg' # TO BUILD DO: Rubinstein Werner_NO_RP2
#Mostly obsolete: Werner (uses NO_RP2) Brandon (I use Werner_NO_RP2)

SWBABY_VERSION = '1.24' # update only when building release

# these values are 0 for normal releases (not all used by all programs)
# build file will have "TEST_ONLY" in name if all not zero (VERIFY)
TESTING = 0         # myexit, fnbase
DEV_DLL = 0         # 0=normal 1=development
ON_NET =  0         # tdt_sys3.rcoDir = 'Z:\\dev\\py-module-dev\\Tdt'
RUN_TESTS_NOW = 0   # run doc (and unit) tests on main

# TO RUN TESTS
# 1. TESTING = 1 # so that tracedumps are seen in IDLE shell (instead of ERROR.txt!)
#    RUN_TESTS_NOW = 1
# 2. with PsychoBaby-Horn.py open in IDLE, hit F5
#    the doc tests will be run
# To debug:
# 3. can call functions from IDLE shell

#
AUTHOR = 'Brandon Warren'
CONTACT_EMAIL = 'Ken_Hancock@meei.harvard.edu' # 'bwarren@uw.edu'