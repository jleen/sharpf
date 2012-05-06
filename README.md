\#f
===

This is a naïve interpreted Scheme implemented in C#.  John wrote it as a
learning project.  Main things I learned were that

- you really can write a functioning Scheme interpreter by porting the Wizard
  Book's metacircular evaluator to the language of your choice,

- `call/cc` is easy if you write your underlying interpreter in CPS, and

- the whole thing is easier than it looks.  I was able to get the entire thing
  (including `call/cc` working in handful of hours, having never implemented
  a language since school).

This is not a complete Scheme, but it handles a subset of the language correctly
and exercises some core ideas like continuations and syntactic transformations.
Randy added a bignum implementation so we have a subset of real R5RS arithmetic.
Cool!
