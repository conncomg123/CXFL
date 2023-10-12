#include "mainwindow.h"

#include <QApplication>


std::unique_ptr<XFLDocument> doc;

int main(int argc, char *argv[])
{
    doc = std::make_unique<XFLDocument>("../test/DOMDocument.xml");
    QApplication a(argc, argv);
    MainWindow w;
    w.show();
    return a.exec();
}
