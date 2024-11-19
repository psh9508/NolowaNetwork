# NolowaNetwork

다양한 네트워크를 한개의 라이브러리에서 관리할 수 있도록 설계 함

## RabbitMQ

메세지 큐 시스템으로 어플리케이션간에 데이터를 비동기적으로 안전하게 전달할 수 있도록 도와줌

### 추가 방법
`Autofac`의 `ContainerBuilder()`를 이용해서 추가한다.

``` C#
var containerBuilder = new ContainerBuilder();
new RabbitMQModule().RegisterModule(containerBuilder);
new RabbitMQModule().SetConfiguration(containerBuilder);
```

`RegisterModule()`을 호출하면 내부적으론 `RabbitMQ` 구동에 필요한 모든 종속성을 등록한다.

### Configuration

`RabbitMQ`를 사용하기 위해서는 `new RabbitMQModule().SetConfiguration()`을 이용해 설정 값을 전달해줘야하는데 형식은 반드시 아래와 같은 형식으로 지정되어야 한다. 내부에선 `MS`의[`IConfiguration`](https://learn.microsoft.com/ko-kr/dotnet/api/microsoft.extensions.configuration.iconfiguration?view=net-8.0) (참고 [.NET의 구성](https://learn.microsoft.com/ko-kr/dotnet/core/extensions/configuration))을 사용해서 받은 설정 값을 사용한다.
``` C#
{
  "Network": {
    "RabbitMQ" {
      "ServerName": "server:1",       // 현재 서버의 이름
      "VirtualHostName": "/",         // 호스트 이름
      "ExchangeName": "Nolowa.topic", // 교환기 이름
      "Address": "localhost",         // 엔드포인트 주소
      "Port" = 6672,                  // 접속 포트
      "UserName" = "admin",           // 접속 아이디
      "Password" = "admin",           // 접속 비밀번호
    }
  }
}
```

### 핵심 모듈

- RabbitNetworkClient

    `Connect()`, `Send<T>()`, `Receive()`과 같은 실제로 **`RabbitMQ`에 종속된 코드**가 있다. 

- MessageMaker

    `RabbitMQ`가 메시지를 보낼 때 필요한 데이터 모델을 만들어주는 객체. 기본적으로는 **메시지를 보내는 서버**, **메시지를 받는 서버** 등의 `RabbitMQ` 메시지의 필수 적인 요소를 채워주는 역할을 한다. 이러한 메시지는 `NetMessageBase`객체로 추상화 되어있어 모든 통신은 `NetMessageBase`객체를 기반으로 진행된다.

- MessageBroker

    실제로 메시지를 보내는 역할을 하는 객체. 내부적으로 비동기 큐인 [Channel](https://learn.microsoft.com/ko-kr/dotnet/core/extensions/channels)을 이용해서 비동기적으로 다른 서버의 메시지를 보내고 **응답을 기다린 후 리턴할 수 있는 함수**와 서버에 메시지를 보내고 **처리 결과를 리턴받지 않는 형태의 함수**가 있다. 이 모듈의 모든 함수는 `NetMessageBase`로 추상화 된 메시지를 받도록 되어있다.

- Worker

    실제 메시지를 보내거나 받을 때 사용하는 `Channel`로 된 비동기 대기가 가능한 버퍼로써 `MessageBroker`는 메시지를 직접 보내지 않고 `Worker`에 넣는 역할만 한다. `Worker`에 넣어진 데이터는 `Worker`의 thread에서 메시지를 다른 서버로 보내거나 본인 서버에서 처리가 필요한 일이면 올바른 Handler로 라우팅 한다.

### 데이터 전송 흐름도

#### 리턴이 필요 없는 데이터 전송의 흐름도

<br>

![](https://github.com/user-attachments/assets/38cf52a7-3c97-406a-8eb7-a779a941f956)


데이터 전송을 `Worker`를 통해 전송하고 받는 측에서는 메시지의 타입을 분석해 올바른 Handler로 라우팅 해서 메시지를 처리한다.

#### 리턴을 받는 데이터 전송의 흐름도

<br>

![](https://github.com/user-attachments/assets/25129e85-5eb6-4377-8560-864f2c1b8321)

데이터를 전송할 때 응답을 받을 `Outbox`를 만들고 응답 데이터가 들어올 때까지 기다린다. 전송 될 데이터는 응답이 필요하다는 플래그를 달고 전송된다. 데이터가 타겟 서버로 전송 후 처리가 완료되면 전송 됐던 곳으로 응답을 보낸다. 전송을 했던 서버는 응답을 받게 되고 메시지에서 응답으로 전송됐다는 플레그가 확인되면 핸들링하지 않고 `Outbox`에 데이터를 넣는다. 데이터가 들어가면 대기가 풀리고 받았던 응답을 사용자에게 리턴한다.


## HTTP

추후 `Http`도 추가 예정