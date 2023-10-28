#include "include/XFLDocument.h"
#include "include/CFile.h"
#include <iostream>
#include <time.h>
int main() {
	XFLDocument xfl("C:\\Stuff\\CXFL\\test\\DOMDocument.xml");
	// std::cout << xfl.getTimeline(0)->getLayer(11)->getFrameCount() << std::endl;
	// // perf test: insert keyframes on every frame in the first layer of the first timeline
	// clock_t start = clock();
	// for (int i = 0; i < xfl.getTimeline(0)->getLayer(11)->getFrameCount(); i++) {
	// 	//std::cout << xfl.getTimeline(0)->getLayer(0)->getFrameCount() << std::endl;
	// 	xfl.getTimeline(0)->getLayer(11)->insertKeyframe(i);
	// }
	// clock_t end = clock();
	// std::cout << "Inserting " << xfl.getTimeline(0)->getLayer(11)->getFrameCount() << " keyframes took " << (double)(end - start) / CLOCKS_PER_SEC << " seconds" << std::endl;
	// // xfl.getTimeline(0)->getLayer(0)->insertKeyframe(6);
	// std::cout << ((SymbolInstance*)(xfl.getTimeline(0)->getLayer(5)->getFrame(0)->getElement(0)))->getMatrix()->getA() << std::endl;
	// ((SymbolInstance*)(xfl.getTimeline(0)->getLayer(5)->getFrame(0)->getElement(0)))->getMatrix()->setA(1337);
	// std::cout << ((SymbolInstance*)(xfl.getTimeline(0)->getLayer(5)->getFrame(0)->getElement(0)))->getMatrix()->getA() << std::endl;
	// xfl.getTimeline(0)->getLayer(0)->insertBlankKeyframe(0);
	// xfl.getTimeline(0)->getLayer(0)->insertBlankKeyframe(1);
	// xfl.getTimeline(0)->getLayer(0)->insertBlankKeyframe(2);
	// xfl.getTimeline(0)->getLayer(0)->insertBlankKeyframe(3);
	// xfl.getTimeline(0)->getLayer(0)->insertBlankKeyframe(4);
	// xfl.getTimeline(0)->getLayer(0)->insertBlankKeyframe(5);
	// xfl.getTimeline(0)->deleteLayer(0);
	// time inserting 1000 new layers
	clock_t start = clock();
	// for (int i = 0; i < 10; i++) {
	// 	xfl.getTimeline(0)->addNewLayer("test");
	// }
	// // insert keyframes on all 
	// for (int i = 0; i < xfl.getTimeline(0)->getLayerCount(); i++) {
	// 	for (int j = 0; j < xfl.getTimeline(0)->getLayer(i)->getFrameCount(); j++) {
	// 		xfl.getTimeline(0)->getLayer(i)->insertKeyframe(j);
	// 	}
	// }
	for (int i = 0; i < 1000; i++) {
		for(int j = 0; j < 1000; j++) {
			xfl.getTimeline(0)->getLayer(0)->getFrame(0)->getElement(0)->getMatrix().setTx(i);
			xfl.getTimeline(0)->getLayer(0)->getFrame(0)->getElement(0)->getMatrix().setTy(j);
		}
	} 
	clock_t end = clock();
	std::cout << "Moving " << 1000 * 1000 << " elements took " << (double)(end - start) / CLOCKS_PER_SEC << " seconds" << std::endl;
	// std::cout << "Inserting " << 10 * xfl.getTimeline(0)->getFrameCount() << " new frames took " << (double)(end - start) / CLOCKS_PER_SEC << " seconds" << std::endl;
	std::cout << (xfl.getTimeline(0)->getLayer(0)->getFrame(0)->getElement(0)->getMatrix().getRoot() == nullptr) << std::endl;
	xfl.getTimeline(0)->getLayer(0)->getFrame(0)->getElement(0)->getMatrix().setTx(0);
	xfl.getTimeline(0)->getLayer(0)->getFrame(0)->getElement(0)->getMatrix().setTy(0);
	std::cout << (xfl.getTimeline(0)->getLayer(0)->getFrame(0)->getElement(0)->getMatrix().getRoot() == nullptr) << std::endl;
	//xfl.getTimeline(0)->addNewLayer("test2");
	xfl.write("C:\\Stuff\\CXFL\\test\\DOMDocument.xml");
	return 0;
}