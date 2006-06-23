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

(define (list-ref n lst)
  (if (= n 0)
      (car lst)
    (list-ref (- n 1) (cdr lst))))
