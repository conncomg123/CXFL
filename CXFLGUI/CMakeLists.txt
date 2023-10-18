cmake_minimum_required(VERSION 3.5)

project(CXFLGUI VERSION 0.1 LANGUAGES CXX)

set(CMAKE_AUTOUIC ON)
set(CMAKE_AUTOMOC ON)
set(CMAKE_AUTORCC ON)
#uncomment when ready for release (optimizema)
#set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -O3")
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

find_package(QT NAMES Qt6 Qt5 REQUIRED COMPONENTS Widgets)
find_package(Qt${QT_VERSION_MAJOR} REQUIRED COMPONENTS Widgets)
#include_directories()
set(PROJECT_SOURCES
        main.cpp
        mainwindow.cpp
        mainwindow.h
        mainwindow.ui
)

if(${QT_VERSION_MAJOR} GREATER_EQUAL 6)
    qt_add_executable(CXFLGUI
        MANUAL_FINALIZATION
        ${PROJECT_SOURCES}
        ../src/CFile.cpp ../src/Element.cpp ../src/Frame.cpp ../src/Layer.cpp ../src/Matrix.cpp ../src/Point.cpp ../src/SymbolInstance.cpp ../src/Timeline.cpp ../src/XFLDocument.cpp
        ../include/CFile.h ../include/Element.h ../include/Frame.h ../include/Layer.h ../include/Matrix.h ../include/Point.h ../include/pugiconfig.hpp ../include/pugixml.hpp ../include/SymbolInstance.h ../include/Timeline.h ../include/XFLDocument.h
        ../lib/libpugixml.lib ../lib/pugiconfig.hpp ../lib/pugixml.cpp ../lib/pugixml.hpp
    )
# Define target properties for Android with Qt 6 as:
#    set_property(TARGET CXFLGUI APPEND PROPERTY QT_ANDROID_PACKAGE_SOURCE_DIR
#                 ${CMAKE_CURRENT_SOURCE_DIR}/android)
# For more information, see https://doc.qt.io/qt-6/qt-add-executable.html#target-creation
else()
    if(ANDROID)
        add_library(CXFLGUI SHARED
            ${PROJECT_SOURCES}
        )
# Define properties for Android with Qt 5 after find_package() calls as:
#    set(ANDROID_PACKAGE_SOURCE_DIR "${CMAKE_CURRENT_SOURCE_DIR}/android")
    else()
        add_executable(CXFLGUI
            ${PROJECT_SOURCES}
        )
    endif()
endif()

target_link_libraries(CXFLGUI PRIVATE Qt${QT_VERSION_MAJOR}::Widgets)

# Qt for iOS sets MACOSX_BUNDLE_GUI_IDENTIFIER automatically since Qt 6.1.
# If you are developing for iOS or macOS you should consider setting an
# explicit, fixed bundle identifier manually though.
if(${QT_VERSION} VERSION_LESS 6.1.0)
  set(BUNDLE_ID_OPTION MACOSX_BUNDLE_GUI_IDENTIFIER com.example.CXFLGUI)
endif()
set_target_properties(CXFLGUI PROPERTIES
    ${BUNDLE_ID_OPTION}
    MACOSX_BUNDLE_BUNDLE_VERSION ${PROJECT_VERSION}
    MACOSX_BUNDLE_SHORT_VERSION_STRING ${PROJECT_VERSION_MAJOR}.${PROJECT_VERSION_MINOR}
    MACOSX_BUNDLE TRUE
    WIN32_EXECUTABLE TRUE
)

include(GNUInstallDirs)
install(TARGETS CXFLGUI
    BUNDLE DESTINATION .
    LIBRARY DESTINATION ${CMAKE_INSTALL_LIBDIR}
    RUNTIME DESTINATION ${CMAKE_INSTALL_BINDIR}
)

if(QT_VERSION_MAJOR EQUAL 6)
    qt_finalize_executable(CXFLGUI)
endif()
