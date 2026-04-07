docker'da rabbitmq kurulumu için eğerki localinizde kurulu bir rabbitmq yoksa aşağıdaki komutu çalıştırın
docker run -d --hostname rabbit --name rabbitmq --restart=always -p 5672:5672 -p 15672:15672 rabbitmq:3-management
http://localhost:15672/ 'den rabbitmqUI arayüzüne erişebilirsiniz. Kod tarafından erişmek için ise 5672 portu kullanılmaktadır.

Api tarafı için ise ilk POST api/orders kullanarak tanımlama yapılır. Ardından GET orders ve GET api/orders/{id} ile eklenen data görüntülenir.
Statulerinin de güncellendiği görülür. Ayrıca Test methodlarından da testi yapılabilir.

