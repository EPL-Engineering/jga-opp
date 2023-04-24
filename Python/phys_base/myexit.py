CONTACT_EMAIL = 'bwarren@u.washington.edu'
version = '2012-03-01'  # version of this module

import os
import sys
import atexit
import time

main_vers = 'unknown'   # version of main program
wx_vers = 'unknown'   # version of wx program

bOkToExit = 0   # used to help detect unexpected exits (e.g. Tracebacks)

# what directory to place ERROR.txt? Must be writable by user.
errorFileName = 'ERROR.txt'
errorFilePath = os.path.abspath(errorFileName)
ORIG_stderr = sys.stderr
bErrorFile = 0
NEW_stderr = 0
try:
    NEW_stderr = open(errorFilePath, 'a')
except Exception, target:
    print 'Unable to open error file. Reason: %s.' % target
    raw_input('press enter to close this window')
    os._exit(0)
else:
    bErrorFile = 1
    sys.stderr = NEW_stderr
    errorFileInitSize = os.stat(errorFilePath).st_size
    #print 'errorFileInitSize=',errorFileInitSize

def RecordVers(mainv, wxv):
    global main_vers
    global wx_vers
    main_vers = mainv
    wx_vers = wxv

def myexit():
    global NEW_stderr
    print '--myexit()--'
    if not bErrorFile:
        print 'no error file'
        raw_input('press enter to close this window')
    else:
        # sys.stderr.write('this should go to file') # good 3-mar
        sys.stderr = ORIG_stderr
        # sys.stderr.write('this should go to console') # good 3-mar
        try:
            NEW_stderr.close()
        except Exception, target:
            print 'ERROR closing stderr:',target
            raw_input('press enter to close this window')
            os._exit(0)
        errorFileCurrSize = os.stat(errorFilePath).st_size
        bErrorFileGrew = errorFileCurrSize > errorFileInitSize
        if bErrorFileGrew:
            try:
                NEW_stderr = open(errorFilePath, 'a')
            except Exception, target:
                print 'Unable to reopen error file. Reason: %s.' % target
            else:
                sys.stderr = NEW_stderr
                sys.stderr.write('\nProgram version = %s\n' % main_vers)
                sys.stderr.write('Python version = %s\n' % sys.version)
                sys.stderr.write('wx version = %s\n' % wx_vers)
                sys.stderr.write(time.strftime('%Y-%m-%d %H:%M:%S', time.localtime()))
                sys.stderr.write('\n-----------------------------\n\n')
                sys.stderr = ORIG_stderr
                try:
                    NEW_stderr.close()
                except Exception, target:
                    print 'ERROR closing stderr:',target
            if bOkToExit:
                print 'info: non-fatal traceback occured'
            errorFileDir = os.path.dirname(errorFilePath)
            # later: would be cool to give option of emailing direct from this program!
            print '\n\nPLEASE send the file named "%s" located in\n %s\n to %s' % (errorFileName, errorFileDir, CONTACT_EMAIL)

            raw_input('press enter to close this window')
        else:
            if not bOkToExit:
                print '\n\nInfo: unexpected exit! PLEASE tell %s' % CONTACT_EMAIL
                raw_input('press enter to close this window')

atexit.register(myexit)
