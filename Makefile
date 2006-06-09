all: sharpf.exe

full: clean all

clean:
	-rm sharpf.exe
	-rm sharpf.pdb

sharpf.exe: sharpf.cs lex.cs parse.cs data.cs env.cs eval.cs prims.cs
	csc /debug $^
