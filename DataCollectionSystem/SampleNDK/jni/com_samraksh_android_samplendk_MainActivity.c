#include "com_samraksh_android_samplendk_MainActivity.h"
#include <stdio.h>   /* Standard input/output definitions */
#include <string.h>  /* String function definitions */
#include <unistd.h>  /* UNIX standard function definitions */
#include <fcntl.h>   /* File control definitions */
#include <errno.h>   /* Error number definitions */
#include <termios.h> /* POSIX terminal control definitions */
#include <android/log.h>

#define APPNAME "SampleNDK"

/*
 * 'open_port()' - Open serial port 1.
 *
 * Returns the file descriptor on success or -1 on error.
 */

JNIEXPORT jint JNICALL Java_com_samraksh_android_samplendk_MainLib_invokeNativeInitialize(JNIEnv *env,jclass clazz)
{
  int fd;  // File descriptor
  int n;
  char *buf;
  buf = (char *)malloc(sizeof(buf));

  fd = open("/dev/ttyHSL2", O_RDWR | O_NOCTTY);
  if (fd == -1){
	  __android_log_print(ANDROID_LOG_VERBOSE, APPNAME, "open_port: Unable to open /dev/ttyHSL2");
  }
  else
	  fcntl(fd, F_SETFL);

  __android_log_print(ANDROID_LOG_VERBOSE, APPNAME, "After opening port. fd = %d\n", fd);

  // Read the configuration of the port
  struct termios options;
  tcgetattr( fd, &options );

  /* Set Baud Rate */
  cfsetispeed( &options, B115200 );
  cfsetospeed( &options, B115200 );

  options.c_cflag |= ( CLOCAL | CREAD );

  // Set the Character size
  options.c_cflag &= ~CSIZE; /* Mask the character size bits */
  options.c_cflag |= CS8;    /* Select 8 data bits */

  // Set parity - No Parity (8N1)
  options.c_cflag &= ~PARENB;
  options.c_cflag &= ~CSTOPB;
  options.c_cflag &= ~CSIZE;
  options.c_cflag |= CS8;

  // Enable Raw Input
  options.c_lflag &= ~(ICANON | ECHO | ECHOE | ISIG);

  // Disable Software Flow control
  options.c_iflag &= ~(IXON | IXOFF | IXANY);

  // Chose raw (not processed) output
  options.c_oflag &= ~OPOST;

  if(tcsetattr( fd, TCSANOW, &options ) == -1 ){
	  __android_log_print(ANDROID_LOG_VERBOSE, APPNAME, "Error with tcsetattr");
  }
  else{
	  __android_log_print(ANDROID_LOG_VERBOSE, APPNAME, "tcsetattr succeed");
  }

  fcntl(fd, F_SETFL);


  // Write some stuff
  while(1)
  {
	  sleep(5);
	  n = write(fd, "AAAA", 4);
	  if (n < 0){
		  __android_log_print(ANDROID_LOG_VERBOSE, APPNAME, "write failed");
	  }
	  else{
		  __android_log_print(ANDROID_LOG_VERBOSE, APPNAME, "write succeeded");
	  }

	  n = read(fd, buf, 1);

	  if (n == -1)
		  __android_log_print(ANDROID_LOG_VERBOSE, APPNAME, "Read error");
	  else
		  __android_log_print(ANDROID_LOG_VERBOSE, APPNAME, "Data read is: %s\n", buf);
  }
  close(fd);
  free(buf);
}

