;; Regression tests

(define (fib n)
  (define (fib-iter a b p q count)
    (cond ((= count 0) b)
          ((even? count)
           (fib-iter a
                     b
                     (+ (* p p) (* 2 p q))
                     (+ (* p p) (* q q))
                     (/ count 2)))
          (else (fib-iter (+ (* (+ a b) p) (* a q))
                          (+ (* a p) (* b q))
                          p
                          q
                          (- count 1)))))
  (fib-iter 1 0 1 0 n))


(define tests
  '(((fib 10) 55)
    ((cons 'a '(b c)) (a b c))))

(define (do-tests tests)
  (let ((total 0) (num-failed 0))
    (for-each
      (lambda (spec)
        (let ((code (car spec))
              (expected (cadr spec)))
          (display code)
          (newline)
          (let ((actual (eval code (current-environment))))
            (if (equal? expected actual)
                (display "Passed!")
                (begin
                  (set! num-failed (+ 1 num-failed))
                  (display "Failed!")
                  (newline)
                  (display "   Expected: ")
                  (display expected)
                  (display "     Actual: ")
                  (display actual)))
            (set! total (+ 1 total))
            (newline)
            (newline))))
      tests)
    (display "Ran ")
    (display total)
    (display " tests")
    (if (= num-failed 0)
        (display " and passed them all!")
        (begin
          (display " but ")
          (display num-failed)
          (display " of them failed.")))
    (newline)))
