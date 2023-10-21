#include "../include/Frame.h"
#include <iostream>
#include <algorithm>
void Frame::loadElements(pugi::xml_node& frameNode) {
	auto elements = frameNode.child("elements").children();
	for (auto iter = elements.begin(); iter != elements.end(); ++iter) {
		std::string type = iter->name();
		if (type.find("SymbolInstance") != std::string::npos) {
			this->elements.push_back(std::make_unique<SymbolInstance>(*iter));
		}
	}
}
Frame::Frame(pugi::xml_node& frameNode, bool isBlank) {
	this->root = frameNode;
	this->startFrame = frameNode.attribute("index").as_uint();
	this->duration = frameNode.attribute("duration").empty() ? 1 : frameNode.attribute("duration").as_uint();
	this->labelType = frameNode.attribute("labelType").empty() ? "none" : frameNode.attribute("labelType").value();
	this->name = frameNode.attribute("name").value();
	if (!isBlank) loadElements(frameNode);
}

// copy constructor, make a deep copy of the frame
Frame::Frame(const Frame& frame, bool isBlank) {
	// use the parent of this->root to insert the copy
	auto parent = frame.root.parent();
	if (!isBlank) {
		this->root = parent.insert_copy_after(frame.root, frame.root);
		loadElements(this->root);
	}
	else {
		this->root = parent.insert_child_after("DOMFrame", frame.root);
	}
	this->setStartFrame(frame.getStartFrame());
	this->setKeyMode(frame.getKeyMode());
	this->setDuration(frame.getDuration());
	this->setLabelType(frame.getLabelType());
	this->setName(frame.getName());
}
Frame::~Frame() {

}
Element* Frame::getElement(unsigned int index) const {
	return elements[index].get();
}
unsigned int Frame::getDuration() const {
	return this->duration;
}
void Frame::setDuration(unsigned int duration) {
	// if duration is 1, we need to remove the attribute if it exists, else we need to set it
	if (duration == 1) this->root.remove_attribute("duration");
	else {
		if (this->root.attribute("duration").empty()) this->root.append_attribute("duration");
		this->root.attribute("duration").set_value(duration);
	}
	this->duration = duration;
}
unsigned int Frame::getStartFrame() const {
	return this->startFrame;
}
void Frame::setStartFrame(unsigned int startFrame) {
	if (this->root.attribute("index").empty()) this->root.append_attribute("index");
	this->root.attribute("index").set_value(startFrame);
	this->startFrame = startFrame;
}
unsigned int Frame::getKeyMode() const {
	return this->keyMode;
}
void Frame::setKeyMode(unsigned int keyMode) {
	if (this->root.attribute("keyMode").empty()) this->root.append_attribute("keyMode");
	this->root.attribute("keyMode").set_value(keyMode);
	this->keyMode = keyMode;
}
std::string Frame::getLabelType() const {
	return this->labelType;
}
void Frame::setLabelType(const std::string& labelType) {
	if (std::find(ACCEPTABLE_LABEL_TYPES.begin(), ACCEPTABLE_LABEL_TYPES.end(), labelType) == ACCEPTABLE_LABEL_TYPES.end()) {
		throw std::invalid_argument("Invalid label type");
	}
	if (this->root.attribute("labelType").empty()) this->root.append_attribute("labelType");
	this->root.attribute("labelType").set_value(labelType.c_str());
	this->labelType = labelType;
}
std::string Frame::getName() const {
	return this->name;
}
void Frame::setName(const std::string& name) {
	if (name.empty()) {
		this->root.remove_attribute("name");
		this->setLabelType("none");
	}
	else {
		if (this->root.attribute("name").empty()) this->root.append_attribute("name");
		this->root.attribute("name").set_value(name.c_str());
		if (this->getLabelType() == "none") this->setLabelType("name");
	}
	this->name = name;
}
bool Frame::isEmpty() const {
	return this->elements.empty();
}
pugi::xml_node& Frame::getRoot() {
	return this->root;
}
void Frame::clearElements() {
	this->elements.clear();
	this->getRoot().remove_child("elements");
}