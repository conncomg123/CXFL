#include "include/XFLDocument.h"
#include "include/CFile.h"
#include <iostream>
#include <time.h>
int main() {
	XFLDocument xfl("C:\\VHC\\301_S5 (1)\\DOMDocument.xml");
	std::cout << xfl.getTimeline(0)->getLayer(11)->getFrameCount() << std::endl;
	// perf test: insert keyframes on every frame in the first layer of the first timeline
	clock_t start = clock();
	for (int i = 0; i < xfl.getTimeline(0)->getLayer(11)->getFrameCount(); i++) {
		//std::cout << xfl.getTimeline(0)->getLayer(0)->getFrameCount() << std::endl;
		xfl.getTimeline(0)->getLayer(11)->insertBlankKeyframe(i);
	}
	clock_t end = clock();
	std::cout << "Inserting " << xfl.getTimeline(0)->getLayer(11)->getFrameCount() << " keyframes took " << (double)(end - start) / CLOCKS_PER_SEC << " seconds" << std::endl;
	// xfl.getTimeline(0)->getLayer(0)->insertKeyframe(6);
	std::cout << ((SymbolInstance*)(xfl.getTimeline(0)->getLayer(5)->getFrame(0)->getElement(0)))->getMatrix()->getA() << std::endl;
	((SymbolInstance*)(xfl.getTimeline(0)->getLayer(5)->getFrame(0)->getElement(0)))->getMatrix()->setA(1337);
	std::cout << ((SymbolInstance*)(xfl.getTimeline(0)->getLayer(5)->getFrame(0)->getElement(0)))->getMatrix()->getA() << std::endl;
	// xfl.getTimeline(0)->getLayer(0)->insertBlankKeyframe(0);
	// xfl.getTimeline(0)->getLayer(0)->insertBlankKeyframe(1);
	// xfl.getTimeline(0)->getLayer(0)->insertBlankKeyframe(2);
	// xfl.getTimeline(0)->getLayer(0)->insertBlankKeyframe(3);
	// xfl.getTimeline(0)->getLayer(0)->insertBlankKeyframe(4);
	// xfl.getTimeline(0)->getLayer(0)->insertBlankKeyframe(5);
	// xfl.write("C:\\VHC\\301_S5 (1)\\DOMDocument.xml");
	return 0;
}