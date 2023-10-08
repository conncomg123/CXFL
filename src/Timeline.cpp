#include "../include/Timeline.h"
#include <stdexcept>
void Timeline::loadLayers(pugi::xml_node& timelineNode) {
	auto layers = timelineNode.child("layers").children("DOMLayer");
	for (auto iter = layers.begin(); iter != layers.end(); ++iter) {
		this->layers.push_back(std::make_unique<Layer>(*iter));
	}
}
Timeline::Timeline(pugi::xml_node& timelineNode) {
	this->root = timelineNode;
	loadLayers(timelineNode);
}
Timeline::~Timeline() {

}
Layer* Timeline::getLayer(unsigned int index) {
	if (index > this->layers.size()) {
		throw std::out_of_range("Layer index out of range");
	}
	return layers[index].get();
}
std::string Timeline::getName() {
	return this->name;
}
void Timeline::setName(const std::string& name) {
	this->root.attribute("name").set_value(name.c_str());
	this->name = name;
}
pugi::xml_node& Timeline::getRoot() {
	return this->root;
}
