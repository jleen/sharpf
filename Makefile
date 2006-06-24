all: tags sharpf.exe

full: clean all

clean:
	-rm sharpf.exe
	-rm sharpf.pdb
	-rm tags

sharpf.exe: \
	data.cs \
	env.cs \
	errors.cs \
	eval.cs \
	lex.cs \
	parse.cs \
	prims.cs \
	sharpf.cs \
	transform.cs \

	csc /debug $^

tags: *.cs *.scm
	ctags $^
