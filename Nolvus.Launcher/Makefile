WIN_CC = x86_64-w64-mingw32-gcc
WIN_FLAGS = -static -static-libgcc

TARGET = NolvusLauncher.exe
SRC = main.c

all: $(TARGET)

$(TARGET): $(SRC)
	$(WIN_CC) $(WIN_FLAGS) -o $(TARGET) $(SRC)

clean:
	rm -f $(TARGET)
