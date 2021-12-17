#include<FastLED.h>
#define NUM_LEDS 3
#define DATA_PIN 23
CRGB leds[NUM_LEDS];

#include <TimeLib.h>
// COM22
int doors[16] = {22, 24, 26, 28, 30, 32, 34, 36, 38, 40, 42, 44, 46, 48, 50, 52} ;
time_t doorsTime[16] = {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0} ;

String cmd;
String cmdHead;
String doorStr;
int door;

//volatile byte relayState = LOW;

void setup() {
  // Pin for relay module set as output
  Serial.begin(9600);
  Serial.println( "Version 1" );
  for (int thisPin = 0; thisPin < 16; thisPin++) {
    pinMode(doors[thisPin], OUTPUT);
    digitalWrite(doors[thisPin], LOW);
    delay(100);
  }
  // Serial communication for debugging purposes
  FastLED.addLeds<NEOPIXEL, DATA_PIN>(leds, NUM_LEDS);
  randomSeed(analogRead(0));
}

void loop() {
  /*
    delay(1000);
    digitalWrite(relay1, LOW);
    Serial.println ("Pin 0 = OFF ");
    digitalWrite(relay2, LOW);
    Serial.println ("Pin 1 = OFF ");
    delay(1000);
    digitalWrite(relay1, HIGH);
    Serial.println ("Pin 0 = OFF ");
    digitalWrite(relay2, HIGH);
    Serial.println ("Pin 1 = OFF ");
  */

  delay(500);
  cmd = Serial.readStringUntil(10);
  cmdHead = cmd.substring(0, 1) ;
  doorStr = cmd.substring(1, 3) ;
  if ( cmdHead == "O" || cmdHead == "C" ) {
    // Serial.println( cmd );
    char carray[doorStr.length() + 1];
    doorStr.toCharArray(carray, sizeof(carray));
    door = atoi(carray);
    //Serial.println( "door : " );
    //Serial.println( door );
    if ( cmdHead == "O" ) {
      openDoor( door ) ;
      Serial.println( "LED On !" );
      LED_ON();

      // new logic : automatically be locked after few seconds
      //delay(10000);

      //delay(200);
      //closeDoor( door ) ;

      doorsTime[door - 1] = now() ;
      Serial.print("door ");
      Serial.print(door);
      Serial.println(" opened !");

    } else if ( cmdHead == "C" ) {
      closeDoor( door ) ;
      LED_OFF();
    }
    //Serial.println("DONE"+doorStr);   // Push each char 1 by 1 on each loop pass
    Serial.println(cmd);
  }

  for (int i = 0; i < 16; i++) {
    if ( doorsTime[i] != 0 ) {
      if ( now() - doorsTime[i] > 15 ) {
        Serial.print("door ");
        Serial.print(i + 1);
        Serial.println(" time out !");
        doorsTime[i] = 0 ;
        closeDoor( i + 1 ) ;
      }
    }
  }

}

void openDoor(int door) {
  digitalWrite(doors[door - 1], HIGH);
  delay(100);
}

void closeDoor(int door) {
  digitalWrite(doors[door - 1], LOW);
  delay(100);
}

void LED_ON() {
//  CRGB leds[NUM_LEDS];
//  FastLED.addLeds<NEOPIXEL, DATA_PIN>(leds, NUM_LEDS);
//  // set random seed
//  randomSeed(analogRead(0));
  for (int cur = 0; cur < NUM_LEDS; cur++) {
    leds[cur] = CRGB(255, 255, 255);
  }
  FastLED.show();
}

void LED_OFF() {
//  CRGB leds[NUM_LEDS];
//  FastLED.addLeds<NEOPIXEL, DATA_PIN>(leds, NUM_LEDS);
//  // set random seed
//  randomSeed(analogRead(0));
  for (int cur = 0; cur < NUM_LEDS; cur++) {
    leds[cur] = CRGB(0, 0, 0);
  }
  FastLED.show();
}
