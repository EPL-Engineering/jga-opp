from ctypes import *
from ctypes.wintypes import DWORD
SIZE_T = c_ulong

class MEMORYSTATUS(Structure):
    # On computers with more than 4 GB of memory, the MEMORYSTATUS structure can return incorrect information.
    # On Intel x86 computers with more than 2 GB and less than 4 GB of memory, the GlobalMemoryStatus function
    # will always return 2 GB in the dwTotalPhys member of the MEMORYSTATUS structure. Similarly, if the total
    # available memory is between 2 and 4 GB, the dwAvailPhys member of the MEMORYSTATUS structure will be rounded down to 2 GB.
    _fields_ = [
        ('dwLength', DWORD),        # size of the MEMORYSTATUS data structure, in bytes. GlobalMemoryStatus sets it.
        ('dwMemoryLoad', DWORD),    # number between 0 and 100 that specifies the approximate percentage of physical memory that is in use
        ('dwTotalPhys', SIZE_T),    # The amount of actual physical memory, in bytes.
        ('dwAvailPhys', SIZE_T),    # The amount of physical memory currently available, in bytes.
        ('dwTotalPageFile', SIZE_T),# This is physical memory plus the size of the page file, minus a small overhead
        ('dwAvailPageFile', SIZE_T),# The maximum amount of memory the current process can commit, in bytes. 
        ('dwTotalVirtual', SIZE_T), # The size of the user-mode portion of the virtual address space of the calling process, in bytes.
        ('dwAvailVirtual', SIZE_T), # The amount of unreserved and uncommitted memory currently in the user-mode portion of the virtual address space of the calling process, in bytes.
        ]

def winmem():
    mstat = MEMORYSTATUS()
    windll.kernel32.GlobalMemoryStatus(byref(mstat))
    return mstat

if __name__ == '__main__':
    m = winmem()
    print '%d%% of memory is in use.' % m.dwMemoryLoad
    print '%d MB phys RAM left out of %d MB total.' % (m.dwAvailPhys/1024**2,
                                                       m.dwTotalPhys/1024**2)
    print '%d MB PF left out of %d MB total.' % (m.dwAvailPageFile/1024**2,
                                                 m.dwTotalPageFile/1024**2)
    print '%d MB virtual RAM left out of %d MB total.' % (m.dwAvailVirtual/1024**2,
                                                          m.dwTotalVirtual/1024**2)
