(define (caar pair)
  (car (car pair)))

(define (cadr pair)
  (car (cdr pair)))

(define (cdar pair)
  (cdr (car pair)))

(define (cddr pair)
  (cdr (cdr pair)))

(define (caddr pair)
  (car (cdr (cdr pair))))

(define (list-ref lst n)
  (if (= n 0)
      (car lst)
    (list-ref (cdr lst) (- n 1) )))

(define (not p)
  (if p #f #t))

(define for-each map)
