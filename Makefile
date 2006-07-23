CSC=gmcs
RUNTIME=mono

all: tags sharpf.exe

full: clean all

clean:
	-rm sharpf.exe
	-rm sharpf.pdb
	-rm tags

sharpf.exe: \
	bignum.cs \
	data.cs \
	env.cs \
	errors.cs \
	eval.cs \
	lex.cs \
	parse.cs \
	prims.cs \
	sharpf.cs \
	transform.cs \

	$(CSC) /debug /out:$@ $^

test: sharpf.exe tests.scm quit.scm
	$(RUNTIME) ./sharpf.exe tests.scm -e "(do-tests tests)" -e "(quit)"

tags: *.cs *.scm
	ctags $^
