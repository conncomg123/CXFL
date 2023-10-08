#ifndef FRAME_H
#define FRAME_H
#include "pugixml.hpp"
#include "Element.h"
#include "SymbolInstance.h"
#include <vector>
#include <memory>
class Frame {
private:
	pugi::xml_node root;
	std::vector<std::unique_ptr<Element>> elements;
	void loadElements(pugi::xml_node& frameNode);
	unsigned int startFrame, duration;
	std::string labelType, name;
public:
	Frame(pugi::xml_node& frameNode, bool isBlank=false);
	Frame(const Frame& frame, bool isBlank=false);
	~Frame();
	Element* getElement(unsigned int index) const;
	unsigned int getDuration() const;
	void setDuration(unsigned int duration);
	unsigned int getStartFrame() const;
	void setStartFrame(unsigned int startFrame);
	std::string getLabelType() const;
	void setLabelType(const std::string& labelType);
	std::string getName() const;
	void setName(const std::string& name);
	bool isEmpty() const;
	pugi::xml_node& getRoot();
};
#endif // FRAME_H