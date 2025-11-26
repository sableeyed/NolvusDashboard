WIN_CC  = x86_64-w64-mingw32-gcc
WIN_RC  = x86_64-w64-mingw32-windres
WIN_FLAGS = -static -static-libgcc

TARGET = NolvusLauncher.exe
SRC    = main.c
RESOBJ = launcher.res.o
ICON   = nolvus-ico.ico
RCFILE = launcher.rc

all: $(TARGET)

$(RESOBJ): $(RCFILE) $(ICON)
	$(WIN_RC) $(RCFILE) -O coff -o $(RESOBJ)

$(TARGET): $(SRC) $(RESOBJ)
	$(WIN_CC) $(WIN_FLAGS) -o $(TARGET) $(SRC) $(RESOBJ)

clean:
	rm -f $(TARGET) $(RESOBJ)
