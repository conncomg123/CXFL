#include "mainwindow.h"
#include "./ui_mainwindow.h"
#include <iostream>

MainWindow::MainWindow(QWidget *parent)
    : QMainWindow(parent)
    , ui(new Ui::MainWindow)
{
    ui->setupUi(this);
}

MainWindow::~MainWindow()
{
    delete ui;
}



void MainWindow::on_timelineButton_clicked()
{

    clock_t start = clock();
	for (int i = 0; i < 1000; i++) {
        doc->getTimeline(0)->addNewLayer("test");
	}
    // insert keyframes on all
    for (int i = 0; i < doc->getTimeline(0)->getLayerCount(); i++) {
        for (int j = 0; j < doc->getTimeline(0)->getLayer(i)->getFrameCount(); j++) {
            doc->getTimeline(0)->getLayer(i)->insertKeyframe(j);
		}
	}
	clock_t end = clock();
    std::cout << "Inserting " << 1000 * doc->getTimeline(0)->getFrameCount() << " new frames took " << (double)(end - start) / CLOCKS_PER_SEC << " seconds" << std::endl;
}
