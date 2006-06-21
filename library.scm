(define cadr
  (lambda (pair)
    (car (cdr pair))))

(define list-ref
  (lambda (n lst)
    (if (= n 0)
        (car lst)
      (list-ref (- n 1) (cdr lst)))))
