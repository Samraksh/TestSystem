LOCAL_PATH:= $(call my-dir)

include $(CLEAR_VARS)

LOCAL_LDLIBS	:= -llog
LOCAL_SRC_FILES	:= com_samraksh_android_samplendk_MainActivity.c
LOCAL_MODULE	:= com_samraksh_android_samplendk_MainActivity

include $(BUILD_SHARED_LIBRARY)