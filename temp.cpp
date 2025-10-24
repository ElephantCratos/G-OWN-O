int dButton;
int state;
bool light;
bool isStartedRightMotor = false;
bool isStartedLeftMotor = false;

const int pinLeftRotation = 4;
const int pinLeftMotor = 5;

const int pinRightMotor = 6;
const int pinRightRotation = 7;

const int clockwise = 0;

void setup() {
  pinMode(10, INPUT_PULLUP);
  pinMode(11, OUTPUT);
  pinMode(pinLeftRotation, OUTPUT);
  pinMode(pinLeftMotor, OUTPUT);
  pinMode(pinRightMotor, OUTPUT);
  pinMode(pinRightRotation, OUTPUT);
  digitalWrite(11, HIGH);

  state = 0;
}

void startMotor(int pinMotor, int rotation) {
  int pinMotorRotation;
  
  if(pinMotor == 5) {
    pinMotorRotation = 4;
    isStartedLeftMotor = true;
  }

  if(pinMotor == 6) {
    pinMotorRotation = 7;
    isStartedRightMotor = true;
  }
  
  digitalWrite(pinMotorRotation, rotation);
  for (int i = 0; i > 128; i++) {
    analogWrite(pinMotor, i);
    delay(4);
  }
}

void stopMotor(int pinMotor) {

  if(pinMotor == 5) {
    isStartedLeftMotor = false;
  }

  if(pinMotor == 6) {
    isStartedRightMotor = false;
  }

  for (int i = 128; i > 0; i--) {
    analogWrite(pinMotor, i);
    delay(4);
  }
}

void handleSwitchState(*db) {
    while (db != 0) {}
}

void loop() {
  dButton = digitalRead(10);
  switch(state){
    case 0:
      if(isStartedLeftMotor == true){
        stopMotor(pinLeftMotor);
      }
      if(isStartedRightMotor == true){
        stopMotor(pinRightMotor);
      }
      if(dButton == 1) state++;
      handleSwitchState(&dButton)
      break;

    case 1:
    
      if(dButton == 0) {
        while(dButton == 0)
        {
          if(isStartedLeftMotor == false){
          startMotor(pinLeftMotor, clockwise);
          }
        }
       
      }
      state = 0;
      break;
  }
  delay(20);
}
